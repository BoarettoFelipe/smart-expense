using Microsoft.EntityFrameworkCore;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;
using SmartExpense.Infrastructure.Persistence;
using SmartExpense.Infrastructure.Persistence.Repositories;

namespace SmartExpense.Tests.Integration.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class TransactionRepositoryTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_StagesTransactionUntilUnitOfWorkSavesIt()
    {
        var userId = Guid.NewGuid();

        await using var dbContext = fixture.CreateDbContext();
        var category = CreateCategory(userId);
        await new CategoryRepository(dbContext).AddAsync(category);
        await ((IUnitOfWork)dbContext).SaveChangesAsync();

        var transaction = CreateTransaction(category.Id, userId);
        var repository = new TransactionRepository(dbContext);
        IUnitOfWork unitOfWork = dbContext;

        Assert.Same(dbContext, unitOfWork);
        Assert.True(fixture.MigrationsApplied);

        await repository.AddAsync(transaction);

        await using (var beforeSaveContext = fixture.CreateDbContext())
        {
            Assert.False(await beforeSaveContext.Transactions.AnyAsync(
                item => item.Id == transaction.Id));
        }

        Assert.Equal(1, await unitOfWork.SaveChangesAsync());

        await using var verificationContext = fixture.CreateDbContext();
        var persisted = await new TransactionRepository(verificationContext)
            .GetByIdAsync(transaction.Id, userId);

        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task GetByIdAsync_WithMatchingIdAndUserId_ReturnsTransaction()
    {
        var userId = Guid.NewGuid();
        var transaction = await PersistTransactionAsync(userId);

        await using var dbContext = fixture.CreateDbContext();
        var result = await new TransactionRepository(dbContext)
            .GetByIdAsync(transaction.Id, userId);

        Assert.NotNull(result);
        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentUserId_ReturnsNull()
    {
        var ownerUserId = Guid.NewGuid();
        var transaction = await PersistTransactionAsync(ownerUserId);

        await using var dbContext = fixture.CreateDbContext();
        var result = await new TransactionRepository(dbContext)
            .GetByIdAsync(transaction.Id, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyRequestedUsersTransactions()
    {
        var requestedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var requestedTransaction = await PersistTransactionAsync(requestedUserId);
        await PersistTransactionAsync(otherUserId);

        await using var dbContext = fixture.CreateDbContext();
        var results = await new TransactionRepository(dbContext)
            .GetByUserAsync(requestedUserId);

        var result = Assert.Single(results);
        Assert.Equal(requestedTransaction.Id, result.Id);
        Assert.Equal(requestedUserId, result.UserId);
    }

    [Fact]
    public async Task Remove_StagesDeletionUntilUnitOfWorkSavesIt()
    {
        var userId = Guid.NewGuid();
        var transaction = await PersistTransactionAsync(userId);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new TransactionRepository(dbContext);
            var persisted = Assert.IsType<Transaction>(
                await repository.GetByIdAsync(transaction.Id, userId));

            repository.Remove(persisted);

            await using (var beforeSaveContext = fixture.CreateDbContext())
            {
                Assert.True(await beforeSaveContext.Transactions.AnyAsync(
                    item => item.Id == transaction.Id));
            }

            Assert.Equal(1, await ((IUnitOfWork)dbContext).SaveChangesAsync());
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.Null(await new TransactionRepository(verificationContext)
            .GetByIdAsync(transaction.Id, userId));
    }

    [Fact]
    public async Task ExistsByCategoryAsync_WithUsersTransaction_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var transaction = await PersistTransactionAsync(userId);

        await using var dbContext = fixture.CreateDbContext();
        var result = await new TransactionRepository(dbContext)
            .ExistsByCategoryAsync(transaction.CategoryId, userId);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByCategoryAsync_WithUnusedCategory_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);

        await using (var setupContext = fixture.CreateDbContext())
        {
            await new CategoryRepository(setupContext).AddAsync(category);
            Assert.Equal(1, await ((IUnitOfWork)setupContext).SaveChangesAsync());
        }

        await using var dbContext = fixture.CreateDbContext();
        var result = await new TransactionRepository(dbContext)
            .ExistsByCategoryAsync(category.Id, userId);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByCategoryAsync_WithAnotherUsersTransaction_ReturnsFalse()
    {
        var categoryOwnerId = Guid.NewGuid();
        var transactionOwnerId = Guid.NewGuid();
        var category = CreateCategory(categoryOwnerId);
        var transaction = CreateTransaction(category.Id, transactionOwnerId);

        await using (var setupContext = fixture.CreateDbContext())
        {
            await new CategoryRepository(setupContext).AddAsync(category);
            await new TransactionRepository(setupContext).AddAsync(transaction);
            Assert.Equal(2, await ((IUnitOfWork)setupContext).SaveChangesAsync());
        }

        await using var dbContext = fixture.CreateDbContext();
        var result = await new TransactionRepository(dbContext)
            .ExistsByCategoryAsync(category.Id, categoryOwnerId);

        Assert.False(result);
    }

    private async Task<Transaction> PersistTransactionAsync(Guid userId)
    {
        await using var dbContext = fixture.CreateDbContext();
        var category = CreateCategory(userId);
        var transaction = CreateTransaction(category.Id, userId);

        await new CategoryRepository(dbContext).AddAsync(category);
        await new TransactionRepository(dbContext).AddAsync(transaction);
        Assert.Equal(2, await ((IUnitOfWork)dbContext).SaveChangesAsync());

        return transaction;
    }

    private static Category CreateCategory(Guid userId)
    {
        return new Category(
            Guid.NewGuid(),
            $"Category-{Guid.NewGuid():N}",
            TransactionType.Expense,
            userId,
            CreatedAt);
    }

    private static Transaction CreateTransaction(Guid categoryId, Guid userId)
    {
        return new Transaction(
            Guid.NewGuid(),
            $"Transaction-{Guid.NewGuid():N}",
            125.50m,
            TransactionType.Expense,
            new DateOnly(2026, 8, 19),
            categoryId,
            userId,
            CreatedAt);
    }
}

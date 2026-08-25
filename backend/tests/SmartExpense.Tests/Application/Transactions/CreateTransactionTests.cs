using SmartExpense.Application.Transactions;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Tests.Application.Transactions;

public sealed class CreateTransactionTests
{
    [Fact]
    public async Task Execute_WithOwnedCategory_CreatesAndPersistsForCurrentUser()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(category);
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateTransaction(
            new StubCurrentUser(userId),
            categoryRepository,
            transactionRepository,
            unitOfWork);

        var beforeCreation = DateTimeOffset.UtcNow;
        var result = await operation.ExecuteAsync(CreateCommand(category.Id));
        var afterCreation = DateTimeOffset.UtcNow;

        Assert.Equal(CreateTransactionStatus.Success, result.Status);
        Assert.NotNull(result.Transaction);
        Assert.NotEqual(Guid.Empty, result.Transaction.Id);
        Assert.InRange(result.Transaction.CreatedAt, beforeCreation, afterCreation);
        Assert.Equal(userId, categoryRepository.LastQueriedUserId);
        var persisted = Assert.Single(transactionRepository.AddedTransactions);
        Assert.Equal(userId, persisted.UserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void Command_DoesNotExposeClientControlledUserId()
    {
        Assert.DoesNotContain(
            typeof(CreateTransactionCommand).GetProperties(),
            property => property.Name == "UserId");
    }

    [Fact]
    public async Task Execute_WithUnavailableCategory_ReturnsCategoryUnavailable()
    {
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateTransaction(
            new StubCurrentUser(Guid.NewGuid()),
            new FakeCategoryRepository(),
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(CreateCommand(Guid.NewGuid()));

        Assert.Equal(CreateTransactionStatus.CategoryUnavailable, result.Status);
        Assert.Empty(transactionRepository.AddedTransactions);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersCategory_ReturnsCategoryUnavailable()
    {
        var currentUserId = Guid.NewGuid();
        var otherUsersCategory = CreateCategory(Guid.NewGuid());
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(otherUsersCategory);
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateTransaction(
            new StubCurrentUser(currentUserId),
            categoryRepository,
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(
            CreateCommand(otherUsersCategory.Id));

        Assert.Equal(CreateTransactionStatus.CategoryUnavailable, result.Status);
        Assert.Equal(currentUserId, categoryRepository.LastQueriedUserId);
        Assert.Empty(transactionRepository.AddedTransactions);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var categoryRepository = new FakeCategoryRepository();
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateTransaction(
            new StubCurrentUser(null),
            categoryRepository,
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(CreateCommand(Guid.NewGuid()));

        Assert.Equal(CreateTransactionStatus.Unauthenticated, result.Status);
        Assert.Equal(0, categoryRepository.GetByIdCallCount);
        Assert.Empty(transactionRepository.AddedTransactions);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithInvalidDomainData_ReturnsInvalidWithoutPersisting()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(category);
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateTransaction(
            new StubCurrentUser(userId),
            categoryRepository,
            transactionRepository,
            unitOfWork);
        var command = CreateCommand(category.Id) with { Amount = 0m };

        var result = await operation.ExecuteAsync(command);

        Assert.Equal(CreateTransactionStatus.Invalid, result.Status);
        Assert.Equal(["Transaction data is invalid."], result.Errors);
        Assert.Empty(transactionRepository.AddedTransactions);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static CreateTransactionCommand CreateCommand(Guid categoryId)
    {
        return new CreateTransactionCommand(
            "Salary",
            2500m,
            TransactionType.Income,
            new DateOnly(2026, 8, 19),
            categoryId);
    }

    private static Category CreateCategory(Guid userId)
    {
        return new Category(
            Guid.NewGuid(),
            $"Category-{Guid.NewGuid():N}",
            TransactionType.Income,
            userId,
            DateTimeOffset.UtcNow);
    }
}

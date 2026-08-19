using Microsoft.EntityFrameworkCore;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;
using SmartExpense.Infrastructure.Persistence.Repositories;

namespace SmartExpense.Tests.Integration.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class BudgetRepositoryTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_StagesBudgetUntilUnitOfWorkSavesIt()
    {
        var userId = Guid.NewGuid();
        var budget = CreateBudget(userId, 8, 2026);

        await using var dbContext = fixture.CreateDbContext();
        var repository = new BudgetRepository(dbContext);
        IUnitOfWork unitOfWork = dbContext;

        Assert.Same(dbContext, unitOfWork);
        Assert.True(fixture.MigrationsApplied);

        await repository.AddAsync(budget);

        await using (var beforeSaveContext = fixture.CreateDbContext())
        {
            Assert.False(await beforeSaveContext.Budgets.AnyAsync(
                item => item.Id == budget.Id));
        }

        Assert.Equal(1, await unitOfWork.SaveChangesAsync());

        await using var verificationContext = fixture.CreateDbContext();
        Assert.NotNull(await new BudgetRepository(verificationContext)
            .GetByIdAsync(budget.Id, userId));
    }

    [Fact]
    public async Task GetByIdAsync_RespectsUserId()
    {
        var ownerUserId = Guid.NewGuid();
        var budget = await PersistBudgetAsync(ownerUserId, 7, 2026);

        await using var dbContext = fixture.CreateDbContext();
        var repository = new BudgetRepository(dbContext);

        var ownerResult = await repository.GetByIdAsync(budget.Id, ownerUserId);
        var otherUserResult = await repository.GetByIdAsync(budget.Id, Guid.NewGuid());

        Assert.NotNull(ownerResult);
        Assert.Equal(budget.Id, ownerResult.Id);
        Assert.Null(otherUserResult);
    }

    [Fact]
    public async Task GetByPeriodAsync_WithMatchingUserAndPeriod_ReturnsBudget()
    {
        var userId = Guid.NewGuid();
        var budget = await PersistBudgetAsync(userId, 6, 2026);

        await using var dbContext = fixture.CreateDbContext();
        var result = await new BudgetRepository(dbContext)
            .GetByPeriodAsync(userId, 6, 2026);

        Assert.NotNull(result);
        Assert.Equal(budget.Id, result.Id);
    }

    [Fact]
    public async Task GetByPeriodAsync_WithDifferentUserId_ReturnsNull()
    {
        var budget = await PersistBudgetAsync(Guid.NewGuid(), 5, 2026);

        await using var dbContext = fixture.CreateDbContext();
        var result = await new BudgetRepository(dbContext)
            .GetByPeriodAsync(Guid.NewGuid(), budget.Month, budget.Year);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyRequestedUsersBudgets()
    {
        var requestedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var requestedBudget = await PersistBudgetAsync(requestedUserId, 4, 2026);
        await PersistBudgetAsync(otherUserId, 4, 2026);

        await using var dbContext = fixture.CreateDbContext();
        var results = await new BudgetRepository(dbContext)
            .GetByUserAsync(requestedUserId);

        var result = Assert.Single(results);
        Assert.Equal(requestedBudget.Id, result.Id);
        Assert.Equal(requestedUserId, result.UserId);
    }

    private async Task<Budget> PersistBudgetAsync(Guid userId, int month, int year)
    {
        await using var dbContext = fixture.CreateDbContext();
        var budget = CreateBudget(userId, month, year);

        await new BudgetRepository(dbContext).AddAsync(budget);
        Assert.Equal(1, await ((IUnitOfWork)dbContext).SaveChangesAsync());

        return budget;
    }

    private static Budget CreateBudget(Guid userId, int month, int year)
    {
        return new Budget(
            Guid.NewGuid(),
            month,
            year,
            500m,
            userId,
            CreatedAt);
    }
}

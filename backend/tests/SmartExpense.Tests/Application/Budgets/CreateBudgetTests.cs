using SmartExpense.Application.Budgets;
using SmartExpense.Domain.Entities;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Budgets;

public sealed class CreateBudgetTests
{
    [Fact]
    public async Task Execute_WithAvailablePeriod_CreatesForCurrentUserAndPersists()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);
        var beforeCreation = DateTimeOffset.UtcNow;

        var result = await operation.ExecuteAsync(CreateCommand());

        var afterCreation = DateTimeOffset.UtcNow;
        Assert.Equal(CreateBudgetStatus.Success, result.Status);
        Assert.NotNull(result.Budget);
        Assert.NotEqual(Guid.Empty, result.Budget.Id);
        Assert.InRange(result.Budget.CreatedAt, beforeCreation, afterCreation);
        var persisted = Assert.Single(repository.AddedBudgets);
        Assert.Equal(userId, persisted.UserId);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void Command_DoesNotExposeClientControlledUserId()
    {
        Assert.DoesNotContain(
            typeof(CreateBudgetCommand).GetProperties(),
            property => property.Name == "UserId");
    }

    [Fact]
    public async Task Execute_WithExistingBudgetForPeriod_ReturnsPeriodConflict()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(CreateBudgetEntity(userId, 8, 2026));
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(CreateCommand());

        Assert.Equal(CreateBudgetStatus.PeriodConflict, result.Status);
        Assert.Empty(repository.AddedBudgets);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithInvalidDomainData_ReturnsStableInvalidResult()
    {
        var repository = new FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateBudget(
            new StubCurrentUser(Guid.NewGuid()),
            repository,
            unitOfWork);
        var command = CreateCommand() with { Amount = 0m };

        var result = await operation.ExecuteAsync(command);

        Assert.Equal(CreateBudgetStatus.Invalid, result.Status);
        Assert.Equal(["Budget data is invalid."], result.Errors);
        Assert.Empty(repository.AddedBudgets);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateBudget(
            new StubCurrentUser(null),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(CreateCommand());

        Assert.Equal(CreateBudgetStatus.Unauthenticated, result.Status);
        Assert.Equal(0, repository.GetByPeriodCallCount);
        Assert.Empty(repository.AddedBudgets);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static CreateBudgetCommand CreateCommand()
    {
        return new CreateBudgetCommand(8, 2026, 500m);
    }

    private static Budget CreateBudgetEntity(Guid userId, int month, int year)
    {
        return new Budget(
            Guid.NewGuid(),
            month,
            year,
            500m,
            userId,
            DateTimeOffset.UtcNow);
    }
}

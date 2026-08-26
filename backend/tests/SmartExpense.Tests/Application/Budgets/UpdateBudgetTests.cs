using SmartExpense.Application.Budgets;
using SmartExpense.Domain.Entities;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Budgets;

public sealed class UpdateBudgetTests
{
    [Fact]
    public async Task Execute_WithAvailablePeriod_UpdatesAndPersists()
    {
        var userId = Guid.NewGuid();
        var budget = CreateBudget(userId, 8, 2026);
        var originalId = budget.Id;
        var originalUserId = budget.UserId;
        var originalCreatedAt = budget.CreatedAt;
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(budget);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);
        var command = new UpdateBudgetCommand(
            budget.Id,
            9,
            2027,
            750m);

        var result = await operation.ExecuteAsync(command);

        Assert.Equal(UpdateBudgetStatus.Success, result.Status);
        Assert.NotNull(result.Budget);
        Assert.Equal(command.Month, result.Budget.Month);
        Assert.Equal(command.Year, result.Budget.Year);
        Assert.Equal(command.Amount, result.Budget.Amount);
        Assert.Equal(originalId, result.Budget.Id);
        Assert.Equal(originalUserId, budget.UserId);
        Assert.Equal(originalCreatedAt, result.Budget.CreatedAt);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void Command_DoesNotExposeClientControlledServerValues()
    {
        var properties = typeof(UpdateBudgetCommand).GetProperties();

        Assert.DoesNotContain(properties, property => property.Name == "UserId");
        Assert.DoesNotContain(properties, property => property.Name == "CreatedAt");
    }

    [Fact]
    public async Task Execute_WithSameBudgetAndPeriod_AllowsUpdate()
    {
        var userId = Guid.NewGuid();
        var budget = CreateBudget(userId, 8, 2026);
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(budget);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateBudgetCommand(
            budget.Id,
            budget.Month,
            budget.Year,
            900m));

        Assert.Equal(UpdateBudgetStatus.Success, result.Status);
        Assert.Equal(900m, result.Budget?.Amount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherBudgetInPeriod_ReturnsPeriodConflict()
    {
        var userId = Guid.NewGuid();
        var budget = CreateBudget(userId, 8, 2026);
        var occupiedPeriodBudget = CreateBudget(userId, 9, 2027);
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(budget);
        repository.Budgets.Add(occupiedPeriodBudget);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateBudgetCommand(
            budget.Id,
            occupiedPeriodBudget.Month,
            occupiedPeriodBudget.Year,
            750m));

        Assert.Equal(UpdateBudgetStatus.PeriodConflict, result.Status);
        Assert.Equal(8, budget.Month);
        Assert.Equal(2026, budget.Year);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WhenBudgetDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateBudget(
            new StubCurrentUser(Guid.NewGuid()),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateBudgetCommand(
            Guid.NewGuid(),
            9,
            2027,
            750m));

        Assert.Equal(UpdateBudgetStatus.NotFound, result.Status);
        Assert.Equal(0, repository.GetByPeriodCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersBudget_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUsersBudget = CreateBudget(Guid.NewGuid(), 8, 2026);
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(otherUsersBudget);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateBudgetCommand(
            otherUsersBudget.Id,
            9,
            2027,
            750m));

        Assert.Equal(UpdateBudgetStatus.NotFound, result.Status);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Equal(0, repository.GetByPeriodCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithInvalidDomainData_ReturnsStableInvalidResult()
    {
        var userId = Guid.NewGuid();
        var budget = CreateBudget(userId, 8, 2026);
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(budget);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateBudgetCommand(
            budget.Id,
            0,
            2027,
            750m));

        Assert.Equal(UpdateBudgetStatus.Invalid, result.Status);
        Assert.Equal(["Budget data is invalid."], result.Errors);
        Assert.Equal(8, budget.Month);
        Assert.Equal(2026, budget.Year);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateBudget(
            new StubCurrentUser(null),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateBudgetCommand(
            Guid.NewGuid(),
            9,
            2027,
            750m));

        Assert.Equal(UpdateBudgetStatus.Unauthenticated, result.Status);
        Assert.Equal(0, repository.GetByIdCallCount);
        Assert.Equal(0, repository.GetByPeriodCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static Budget CreateBudget(Guid userId, int month, int year)
    {
        return new Budget(
            Guid.NewGuid(),
            month,
            year,
            500m,
            userId,
            new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));
    }
}

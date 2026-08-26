using SmartExpense.Application.Budgets;
using SmartExpense.Domain.Entities;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Budgets;

public sealed class DeleteBudgetTests
{
    [Fact]
    public async Task Execute_WithOwnedBudget_RemovesAndPersists()
    {
        var userId = Guid.NewGuid();
        var budget = CreateBudget(userId);
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(budget);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(budget.Id);

        Assert.Equal(DeleteBudgetStatus.Success, result.Status);
        Assert.Same(budget, Assert.Single(repository.RemovedBudgets));
        Assert.Empty(repository.Budgets);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WhenBudgetDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteBudget(
            new StubCurrentUser(Guid.NewGuid()),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(DeleteBudgetStatus.NotFound, result.Status);
        Assert.Empty(repository.RemovedBudgets);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersBudget_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUsersBudget = CreateBudget(Guid.NewGuid());
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(otherUsersBudget);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteBudget(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(otherUsersBudget.Id);

        Assert.Equal(DeleteBudgetStatus.NotFound, result.Status);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Empty(repository.RemovedBudgets);
        Assert.Contains(otherUsersBudget, repository.Budgets);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeBudgetRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteBudget(
            new StubCurrentUser(null),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(DeleteBudgetStatus.Unauthenticated, result.Status);
        Assert.Equal(0, repository.GetByIdCallCount);
        Assert.Empty(repository.RemovedBudgets);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static Budget CreateBudget(Guid userId)
    {
        return new Budget(
            Guid.NewGuid(),
            8,
            2026,
            500m,
            userId,
            DateTimeOffset.UtcNow);
    }
}

using SmartExpense.Application.Budgets;
using SmartExpense.Domain.Entities;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Budgets;

public sealed class GetBudgetByIdTests
{
    [Fact]
    public async Task Execute_WithOwnedBudget_ReturnsBudget()
    {
        var userId = Guid.NewGuid();
        var budget = CreateBudget(userId);
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(budget);
        var operation = new GetBudgetById(
            new StubCurrentUser(userId),
            repository);

        var result = await operation.ExecuteAsync(budget.Id);

        Assert.Equal(GetBudgetByIdStatus.Success, result.Status);
        Assert.Equal(budget.Id, result.Budget?.Id);
        Assert.Equal(budget.Id, repository.LastQueriedBudgetId);
        Assert.Equal(userId, repository.LastQueriedUserId);
    }

    [Fact]
    public async Task Execute_WhenBudgetDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeBudgetRepository();
        var operation = new GetBudgetById(
            new StubCurrentUser(Guid.NewGuid()),
            repository);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(GetBudgetByIdStatus.NotFound, result.Status);
        Assert.Null(result.Budget);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersBudget_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUsersBudget = CreateBudget(Guid.NewGuid());
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(otherUsersBudget);
        var operation = new GetBudgetById(
            new StubCurrentUser(userId),
            repository);

        var result = await operation.ExecuteAsync(otherUsersBudget.Id);

        Assert.Equal(GetBudgetByIdStatus.NotFound, result.Status);
        Assert.Null(result.Budget);
        Assert.Equal(userId, repository.LastQueriedUserId);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeBudgetRepository();
        var operation = new GetBudgetById(
            new StubCurrentUser(null),
            repository);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(GetBudgetByIdStatus.Unauthenticated, result.Status);
        Assert.Null(result.Budget);
        Assert.Equal(0, repository.GetByIdCallCount);
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

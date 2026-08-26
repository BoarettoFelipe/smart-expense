using SmartExpense.Application.Budgets;
using SmartExpense.Domain.Entities;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Budgets;

public sealed class GetBudgetsTests
{
    [Fact]
    public async Task Execute_ReturnsOnlyCurrentUsersRepositoryResults()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeBudgetRepository();
        var expected = CreateBudget(userId);
        repository.Budgets.Add(expected);
        repository.Budgets.Add(CreateBudget(Guid.NewGuid()));
        var operation = new GetBudgets(
            new StubCurrentUser(userId),
            repository);

        var result = await operation.ExecuteAsync();

        Assert.Equal(GetBudgetsStatus.Success, result.Status);
        var budget = Assert.Single(result.Budgets);
        Assert.Equal(expected.Id, budget.Id);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Equal(1, repository.GetByUserCallCount);
        Assert.DoesNotContain(
            typeof(BudgetModel).GetProperties(),
            property => property.Name == "UserId");
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeBudgetRepository();
        var operation = new GetBudgets(
            new StubCurrentUser(null),
            repository);

        var result = await operation.ExecuteAsync();

        Assert.Equal(GetBudgetsStatus.Unauthenticated, result.Status);
        Assert.Empty(result.Budgets);
        Assert.Equal(0, repository.GetByUserCallCount);
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

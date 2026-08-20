using SmartExpense.Application.Transactions;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Tests.Application.Transactions;

public sealed class GetTransactionsTests
{
    [Fact]
    public async Task Execute_ReturnsOnlyCurrentUsersRepositoryResults()
    {
        var currentUserId = Guid.NewGuid();
        var repository = new FakeTransactionRepository();
        var expected = CreateTransaction(currentUserId);
        repository.Transactions.Add(expected);
        repository.Transactions.Add(CreateTransaction(Guid.NewGuid()));
        var operation = new GetTransactions(
            new StubCurrentUser(currentUserId),
            repository);

        var result = await operation.ExecuteAsync();

        Assert.Equal(GetTransactionsStatus.Success, result.Status);
        var transaction = Assert.Single(result.Transactions);
        Assert.Equal(expected.Id, transaction.Id);
        Assert.Equal(currentUserId, repository.LastQueriedUserId);
        Assert.Equal(1, repository.GetByUserCallCount);
        Assert.DoesNotContain(
            typeof(TransactionModel).GetProperties(),
            property => property.Name == "UserId");
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeTransactionRepository();
        var operation = new GetTransactions(
            new StubCurrentUser(null),
            repository);

        var result = await operation.ExecuteAsync();

        Assert.Equal(GetTransactionsStatus.Unauthenticated, result.Status);
        Assert.Empty(result.Transactions);
        Assert.Equal(0, repository.GetByUserCallCount);
    }

    private static Transaction CreateTransaction(Guid userId)
    {
        return new Transaction(
            Guid.NewGuid(),
            $"Transaction-{Guid.NewGuid():N}",
            100m,
            TransactionType.Expense,
            new DateOnly(2026, 8, 19),
            Guid.NewGuid(),
            userId,
            DateTimeOffset.UtcNow);
    }
}

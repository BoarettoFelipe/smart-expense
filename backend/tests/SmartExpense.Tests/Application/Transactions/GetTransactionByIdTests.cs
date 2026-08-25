using SmartExpense.Application.Transactions;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Tests.Application.Transactions;

public sealed class GetTransactionByIdTests
{
    [Fact]
    public async Task Execute_WithOwnedTransaction_ReturnsTransaction()
    {
        var currentUserId = Guid.NewGuid();
        var transaction = CreateTransaction(currentUserId);
        var repository = new FakeTransactionRepository();
        repository.Transactions.Add(transaction);
        var operation = new GetTransactionById(
            new StubCurrentUser(currentUserId),
            repository);

        var result = await operation.ExecuteAsync(transaction.Id);

        Assert.Equal(GetTransactionByIdStatus.Success, result.Status);
        Assert.Equal(transaction.Id, result.Transaction?.Id);
        Assert.Equal(currentUserId, repository.LastQueriedUserId);
    }

    [Fact]
    public async Task Execute_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        var currentUserId = Guid.NewGuid();
        var repository = new FakeTransactionRepository();
        var operation = new GetTransactionById(
            new StubCurrentUser(currentUserId),
            repository);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(GetTransactionByIdStatus.NotFound, result.Status);
        Assert.Null(result.Transaction);
        Assert.Equal(currentUserId, repository.LastQueriedUserId);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersTransaction_ReturnsNotFound()
    {
        var currentUserId = Guid.NewGuid();
        var otherUsersTransaction = CreateTransaction(Guid.NewGuid());
        var repository = new FakeTransactionRepository();
        repository.Transactions.Add(otherUsersTransaction);
        var operation = new GetTransactionById(
            new StubCurrentUser(currentUserId),
            repository);

        var result = await operation.ExecuteAsync(otherUsersTransaction.Id);

        Assert.Equal(GetTransactionByIdStatus.NotFound, result.Status);
        Assert.Null(result.Transaction);
        Assert.Equal(currentUserId, repository.LastQueriedUserId);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeTransactionRepository();
        var operation = new GetTransactionById(
            new StubCurrentUser(null),
            repository);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(GetTransactionByIdStatus.Unauthenticated, result.Status);
        Assert.Null(result.Transaction);
        Assert.Equal(0, repository.GetByIdCallCount);
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

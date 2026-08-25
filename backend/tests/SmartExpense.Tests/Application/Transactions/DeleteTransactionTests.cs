using SmartExpense.Application.Transactions;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Tests.Application.Transactions;

public sealed class DeleteTransactionTests
{
    [Fact]
    public async Task Execute_WithOwnedTransaction_RemovesAndPersists()
    {
        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId);
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(transaction);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(transaction.Id);

        Assert.Equal(DeleteTransactionStatus.Success, result.Status);
        Assert.Same(transaction, Assert.Single(
            transactionRepository.RemovedTransactions));
        Assert.Empty(transactionRepository.Transactions);
        Assert.Equal(transaction.Id, transactionRepository.LastQueriedTransactionId);
        Assert.Equal(userId, transactionRepository.LastQueriedUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteTransaction(
            new StubCurrentUser(Guid.NewGuid()),
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(DeleteTransactionStatus.NotFound, result.Status);
        Assert.Empty(transactionRepository.RemovedTransactions);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersTransaction_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUsersTransaction = CreateTransaction(Guid.NewGuid());
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(otherUsersTransaction);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(otherUsersTransaction.Id);

        Assert.Equal(DeleteTransactionStatus.NotFound, result.Status);
        Assert.Equal(userId, transactionRepository.LastQueriedUserId);
        Assert.Empty(transactionRepository.RemovedTransactions);
        Assert.Contains(otherUsersTransaction, transactionRepository.Transactions);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteTransaction(
            new StubCurrentUser(null),
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(DeleteTransactionStatus.Unauthenticated, result.Status);
        Assert.Equal(0, transactionRepository.GetByIdCallCount);
        Assert.Empty(transactionRepository.RemovedTransactions);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static Transaction CreateTransaction(Guid userId)
    {
        return new Transaction(
            Guid.NewGuid(),
            "Transaction",
            100m,
            TransactionType.Expense,
            new DateOnly(2026, 8, 25),
            Guid.NewGuid(),
            userId,
            DateTimeOffset.UtcNow);
    }
}

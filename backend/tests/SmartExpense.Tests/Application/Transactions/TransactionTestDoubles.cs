using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;

namespace SmartExpense.Tests.Application.Transactions;

internal sealed class StubCurrentUser(Guid? userId) : ICurrentUser
{
    public Guid? UserId { get; } = userId;
}

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> Categories { get; } = [];

    public int GetByIdCallCount { get; private set; }

    public Guid? LastQueriedUserId { get; private set; }

    public Task<Category?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        GetByIdCallCount++;
        LastQueriedUserId = userId;

        return Task.FromResult(Categories.SingleOrDefault(
            category => category.Id == id && category.UserId == userId));
    }

    public Task<IReadOnlyList<Category>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Category> categories = Categories
            .Where(category => category.UserId == userId)
            .ToArray();

        return Task.FromResult(categories);
    }

    public Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        Categories.Add(category);
        return Task.CompletedTask;
    }

    public void Remove(Category category)
    {
        Categories.Remove(category);
    }
}

internal sealed class FakeTransactionRepository : ITransactionRepository
{
    public List<Transaction> Transactions { get; } = [];

    public List<Transaction> AddedTransactions { get; } = [];

    public List<Transaction> RemovedTransactions { get; } = [];

    public int GetByIdCallCount { get; private set; }

    public int GetByUserCallCount { get; private set; }

    public Guid? LastQueriedUserId { get; private set; }

    public Guid? LastQueriedTransactionId { get; private set; }

    public Task<Transaction?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        GetByIdCallCount++;
        LastQueriedUserId = userId;
        LastQueriedTransactionId = id;

        return Task.FromResult(Transactions.SingleOrDefault(
            transaction => transaction.Id == id && transaction.UserId == userId));
    }

    public Task<IReadOnlyList<Transaction>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        GetByUserCallCount++;
        LastQueriedUserId = userId;

        IReadOnlyList<Transaction> transactions = Transactions
            .Where(transaction => transaction.UserId == userId)
            .ToArray();

        return Task.FromResult(transactions);
    }

    public Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        AddedTransactions.Add(transaction);
        Transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public void Remove(Transaction transaction)
    {
        RemovedTransactions.Add(transaction);
        Transactions.Remove(transaction);
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }
}

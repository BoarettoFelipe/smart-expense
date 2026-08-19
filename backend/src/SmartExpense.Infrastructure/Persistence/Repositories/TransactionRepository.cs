using Microsoft.EntityFrameworkCore;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;

namespace SmartExpense.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository(SmartExpenseDbContext dbContext)
    : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Transactions.SingleOrDefaultAsync(
            transaction => transaction.Id == id && transaction.UserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }

    public void Remove(Transaction transaction)
    {
        dbContext.Transactions.Remove(transaction);
    }
}

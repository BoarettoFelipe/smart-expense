using SmartExpense.Domain.Entities;

namespace SmartExpense.Application.Abstractions.Persistence;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    void Remove(Transaction transaction);
}

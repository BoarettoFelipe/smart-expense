using SmartExpense.Domain.Entities;

namespace SmartExpense.Application.Abstractions.Persistence;

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Budget?> GetByPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Budget>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Budget budget,
        CancellationToken cancellationToken = default);

    void Remove(Budget budget);
}

using Microsoft.EntityFrameworkCore;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;

namespace SmartExpense.Infrastructure.Persistence.Repositories;

public sealed class BudgetRepository(SmartExpenseDbContext dbContext)
    : IBudgetRepository
{
    public Task<Budget?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Budgets.SingleOrDefaultAsync(
            budget => budget.Id == id && budget.UserId == userId,
            cancellationToken);
    }

    public Task<Budget?> GetByPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Budgets.SingleOrDefaultAsync(
            budget => budget.UserId == userId &&
                budget.Month == month &&
                budget.Year == year,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Budget>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Budgets
            .AsNoTracking()
            .Where(budget => budget.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Budget budget,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Budgets.AddAsync(budget, cancellationToken);
    }

    public void Remove(Budget budget)
    {
        dbContext.Budgets.Remove(budget);
    }
}

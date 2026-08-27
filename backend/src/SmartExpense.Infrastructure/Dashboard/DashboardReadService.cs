using Microsoft.EntityFrameworkCore;
using SmartExpense.Application.Abstractions.Dashboard;
using SmartExpense.Domain.Enums;
using SmartExpense.Infrastructure.Persistence;

namespace SmartExpense.Infrastructure.Dashboard;

public sealed class DashboardReadService(SmartExpenseDbContext dbContext)
    : IDashboardReadService
{
    public async Task<DashboardReadData> GetMonthlyAsync(
        Guid userId,
        int month,
        int year,
        DateOnly startDate,
        DateOnly? endDateExclusive,
        CancellationToken cancellationToken = default)
    {
        var transactions = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.Date >= startDate);

        if (endDateExclusive is DateOnly endDate)
        {
            transactions = transactions.Where(transaction =>
                transaction.Date < endDate);
        }

        var summary = await transactions
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalIncome = group.Sum(transaction =>
                    transaction.Type == TransactionType.Income
                        ? transaction.Amount
                        : 0m),
                TotalExpenses = group.Sum(transaction =>
                    transaction.Type == TransactionType.Expense
                        ? transaction.Amount
                        : 0m),
                TransactionCount = group.Count()
            })
            .SingleOrDefaultAsync(cancellationToken);

        var categoryAggregates = await (
            from transaction in transactions
            join category in dbContext.Categories.AsNoTracking()
                    .Where(category => category.UserId == userId)
                on transaction.CategoryId equals category.Id
            where transaction.Type == TransactionType.Expense
            group transaction by new
            {
                category.Id,
                category.Name
            }
            into categoryGroup
            select new
            {
                CategoryId = categoryGroup.Key.Id,
                CategoryName = categoryGroup.Key.Name,
                Amount = categoryGroup.Sum(transaction => transaction.Amount)
            })
            .OrderByDescending(category => category.Amount)
            .ToListAsync(cancellationToken);

        var expensesByCategory = categoryAggregates
            .Select(category => new DashboardCategoryExpenseReadData(
                category.CategoryId,
                category.CategoryName,
                category.Amount))
            .ToArray();

        var dailyAggregates = await transactions
            .GroupBy(transaction => transaction.Date)
            .Select(dayGroup => new
            {
                Date = dayGroup.Key,
                Income = dayGroup.Sum(transaction =>
                    transaction.Type == TransactionType.Income
                        ? transaction.Amount
                        : 0m),
                Expense = dayGroup.Sum(transaction =>
                    transaction.Type == TransactionType.Expense
                        ? transaction.Amount
                        : 0m)
            })
            .OrderBy(day => day.Date)
            .ToListAsync(cancellationToken);

        var dailyFlow = dailyAggregates
            .Select(day => new DashboardDailyFlowReadData(
                day.Date,
                day.Income,
                day.Expense))
            .ToArray();

        var budgetAmount = await dbContext.Budgets
            .AsNoTracking()
            .Where(budget =>
                budget.UserId == userId &&
                budget.Month == month &&
                budget.Year == year)
            .Select(budget => (decimal?)budget.Amount)
            .SingleOrDefaultAsync(cancellationToken);

        return new DashboardReadData(
            summary?.TotalIncome ?? 0m,
            summary?.TotalExpenses ?? 0m,
            summary?.TransactionCount ?? 0,
            budgetAmount,
            expensesByCategory,
            dailyFlow);
    }
}

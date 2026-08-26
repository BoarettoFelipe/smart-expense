using SmartExpense.Domain.Entities;

namespace SmartExpense.Application.Budgets;

public sealed record BudgetModel(
    Guid Id,
    int Month,
    int Year,
    decimal Amount,
    DateTimeOffset CreatedAt)
{
    internal static BudgetModel FromEntity(Budget budget)
    {
        return new BudgetModel(
            budget.Id,
            budget.Month,
            budget.Year,
            budget.Amount,
            budget.CreatedAt);
    }
}

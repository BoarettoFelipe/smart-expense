namespace SmartExpense.Application.Abstractions.Dashboard;

public interface IDashboardReadService
{
    Task<DashboardReadData> GetMonthlyAsync(
        Guid userId,
        int month,
        int year,
        DateOnly startDate,
        DateOnly? endDateExclusive,
        CancellationToken cancellationToken = default);
}

public sealed record DashboardReadData(
    decimal TotalIncome,
    decimal TotalExpenses,
    int TransactionCount,
    decimal? BudgetAmount,
    IReadOnlyList<DashboardCategoryExpenseReadData> ExpensesByCategory,
    IReadOnlyList<DashboardDailyFlowReadData> DailyFlow);

public sealed record DashboardCategoryExpenseReadData(
    Guid CategoryId,
    string CategoryName,
    decimal Amount);

public sealed record DashboardDailyFlowReadData(
    DateOnly Date,
    decimal Income,
    decimal Expense);

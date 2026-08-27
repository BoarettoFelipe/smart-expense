namespace SmartExpense.Api.Contracts.Dashboard;

public sealed record DashboardResponse(
    int Month,
    int Year,
    DashboardSummaryResponse Summary,
    DashboardBudgetResponse? Budget,
    IReadOnlyList<DashboardCategoryExpenseResponse> ExpensesByCategory,
    IReadOnlyList<DashboardDailyFlowResponse> DailyFlow);

public sealed record DashboardSummaryResponse(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance,
    int TransactionCount);

public sealed record DashboardBudgetResponse(
    decimal Amount,
    decimal Spent,
    decimal Remaining,
    decimal PercentageUsed,
    bool IsExceeded);

public sealed record DashboardCategoryExpenseResponse(
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    decimal PercentageOfTotalExpenses);

public sealed record DashboardDailyFlowResponse(
    DateOnly Date,
    decimal Income,
    decimal Expense,
    decimal Net);

public sealed record DashboardErrorResponse(
    string Message,
    IReadOnlyCollection<string>? Errors = null);

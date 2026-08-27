namespace SmartExpense.Application.Dashboard;

public sealed record DashboardModel(
    int Month,
    int Year,
    DashboardSummaryModel Summary,
    DashboardBudgetModel? Budget,
    IReadOnlyList<DashboardCategoryExpenseModel> ExpensesByCategory,
    IReadOnlyList<DashboardDailyFlowModel> DailyFlow);

public sealed record DashboardSummaryModel(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance,
    int TransactionCount);

public sealed record DashboardBudgetModel(
    decimal Amount,
    decimal Spent,
    decimal Remaining,
    decimal PercentageUsed,
    bool IsExceeded);

public sealed record DashboardCategoryExpenseModel(
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    decimal PercentageOfTotalExpenses);

public sealed record DashboardDailyFlowModel(
    DateOnly Date,
    decimal Income,
    decimal Expense,
    decimal Net);

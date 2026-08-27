using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Dashboard;

namespace SmartExpense.Application.Dashboard;

public sealed class GetDashboard(
    ICurrentUser currentUser,
    IDashboardReadService dashboardReadService)
{
    public async Task<GetDashboardResult> ExecuteAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePeriod(month, year, out var startDate, out var endDate))
        {
            return GetDashboardResult.InvalidPeriod();
        }

        if (currentUser.UserId is not Guid userId)
        {
            return GetDashboardResult.Unauthenticated();
        }

        var readData = await dashboardReadService.GetMonthlyAsync(
            userId,
            month,
            year,
            startDate,
            endDate,
            cancellationToken);

        var summary = new DashboardSummaryModel(
            readData.TotalIncome,
            readData.TotalExpenses,
            readData.TotalIncome - readData.TotalExpenses,
            readData.TransactionCount);

        var budget = readData.BudgetAmount is decimal budgetAmount
            ? new DashboardBudgetModel(
                budgetAmount,
                readData.TotalExpenses,
                budgetAmount - readData.TotalExpenses,
                readData.TotalExpenses / budgetAmount * 100m,
                readData.TotalExpenses > budgetAmount)
            : null;

        var expensesByCategory = readData.TotalExpenses == 0m
            ? []
            : readData.ExpensesByCategory
                .OrderByDescending(category => category.Amount)
                .Select(category => new DashboardCategoryExpenseModel(
                    category.CategoryId,
                    category.CategoryName,
                    category.Amount,
                    category.Amount / readData.TotalExpenses * 100m))
                .ToArray();

        var dailyFlow = readData.DailyFlow
            .OrderBy(day => day.Date)
            .Select(day => new DashboardDailyFlowModel(
                day.Date,
                day.Income,
                day.Expense,
                day.Income - day.Expense))
            .ToArray();

        return GetDashboardResult.Success(new DashboardModel(
            month,
            year,
            summary,
            budget,
            expensesByCategory,
            dailyFlow));
    }

    private static bool TryCreatePeriod(
        int month,
        int year,
        out DateOnly startDate,
        out DateOnly? endDateExclusive)
    {
        startDate = default;
        endDateExclusive = default;

        if (month is < 1 or > 12 || year <= 0)
        {
            return false;
        }

        try
        {
            startDate = new DateOnly(year, month, 1);
            endDateExclusive = year == DateOnly.MaxValue.Year && month == 12
                ? null
                : startDate.AddMonths(1);

            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}

public enum GetDashboardStatus
{
    Success,
    Unauthenticated,
    InvalidPeriod
}

public sealed record GetDashboardResult(
    GetDashboardStatus Status,
    DashboardModel? Dashboard,
    IReadOnlyCollection<string> Errors)
{
    public static GetDashboardResult Success(DashboardModel dashboard) =>
        new(GetDashboardStatus.Success, dashboard, []);

    public static GetDashboardResult Unauthenticated() =>
        new(GetDashboardStatus.Unauthenticated, null, []);

    public static GetDashboardResult InvalidPeriod() =>
        new(
            GetDashboardStatus.InvalidPeriod,
            null,
            ["Dashboard period is invalid."]);
}

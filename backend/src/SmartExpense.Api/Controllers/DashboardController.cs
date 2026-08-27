using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartExpense.Api.Contracts.Dashboard;
using SmartExpense.Application.Dashboard;

namespace SmartExpense.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(GetDashboard getDashboard) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var result = await getDashboard.ExecuteAsync(
            month,
            year,
            cancellationToken);

        if (result.Status == GetDashboardStatus.Unauthenticated)
        {
            return Unauthorized();
        }

        if (result.Status == GetDashboardStatus.InvalidPeriod)
        {
            return BadRequest(new DashboardErrorResponse(
                "Dashboard period is invalid.",
                result.Errors));
        }

        return Ok(Map(result.Dashboard!));
    }

    private static DashboardResponse Map(DashboardModel dashboard)
    {
        return new DashboardResponse(
            dashboard.Month,
            dashboard.Year,
            new DashboardSummaryResponse(
                dashboard.Summary.TotalIncome,
                dashboard.Summary.TotalExpenses,
                dashboard.Summary.Balance,
                dashboard.Summary.TransactionCount),
            dashboard.Budget is null
                ? null
                : new DashboardBudgetResponse(
                    dashboard.Budget.Amount,
                    dashboard.Budget.Spent,
                    dashboard.Budget.Remaining,
                    dashboard.Budget.PercentageUsed,
                    dashboard.Budget.IsExceeded),
            dashboard.ExpensesByCategory
                .Select(category => new DashboardCategoryExpenseResponse(
                    category.CategoryId,
                    category.CategoryName,
                    category.Amount,
                    category.PercentageOfTotalExpenses))
                .ToArray(),
            dashboard.DailyFlow
                .Select(day => new DashboardDailyFlowResponse(
                    day.Date,
                    day.Income,
                    day.Expense,
                    day.Net))
                .ToArray());
    }
}

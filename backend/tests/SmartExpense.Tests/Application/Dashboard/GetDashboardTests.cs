using SmartExpense.Application.Abstractions.Dashboard;
using SmartExpense.Application.Dashboard;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Dashboard;

public sealed class GetDashboardTests
{
    [Fact]
    public async Task Execute_WithDashboardData_ReturnsCalculatedMonthlyDashboard()
    {
        var userId = Guid.NewGuid();
        var groceriesId = Guid.NewGuid();
        var transportId = Guid.NewGuid();
        var readService = new FakeDashboardReadService
        {
            Data = new DashboardReadData(
                3_000m,
                350m,
                3,
                1_000m,
                [
                    new(transportId, "Transport", 150m),
                    new(groceriesId, "Groceries", 200m)
                ],
                [
                    new(new DateOnly(2026, 8, 3), 0m, 150m),
                    new(new DateOnly(2026, 8, 1), 3_000m, 200m)
                ])
        };
        var operation = new GetDashboard(
            new StubCurrentUser(userId),
            readService);

        var result = await operation.ExecuteAsync(8, 2026);

        Assert.Equal(GetDashboardStatus.Success, result.Status);
        var dashboard = Assert.IsType<DashboardModel>(result.Dashboard);
        Assert.Equal(8, dashboard.Month);
        Assert.Equal(2026, dashboard.Year);
        Assert.Equal(3_000m, dashboard.Summary.TotalIncome);
        Assert.Equal(350m, dashboard.Summary.TotalExpenses);
        Assert.Equal(2_650m, dashboard.Summary.Balance);
        Assert.Equal(3, dashboard.Summary.TransactionCount);

        var budget = Assert.IsType<DashboardBudgetModel>(dashboard.Budget);
        Assert.Equal(1_000m, budget.Amount);
        Assert.Equal(350m, budget.Spent);
        Assert.Equal(650m, budget.Remaining);
        Assert.Equal(35m, budget.PercentageUsed);
        Assert.False(budget.IsExceeded);

        Assert.Collection(
            dashboard.ExpensesByCategory,
            groceries =>
            {
                Assert.Equal(groceriesId, groceries.CategoryId);
                Assert.Equal("Groceries", groceries.CategoryName);
                Assert.Equal(200m, groceries.Amount);
                Assert.Equal(57.14m, groceries.PercentageOfTotalExpenses, 2);
            },
            transport =>
            {
                Assert.Equal(transportId, transport.CategoryId);
                Assert.Equal(150m, transport.Amount);
                Assert.Equal(42.86m, transport.PercentageOfTotalExpenses, 2);
            });

        Assert.Collection(
            dashboard.DailyFlow,
            firstDay =>
            {
                Assert.Equal(new DateOnly(2026, 8, 1), firstDay.Date);
                Assert.Equal(3_000m, firstDay.Income);
                Assert.Equal(200m, firstDay.Expense);
                Assert.Equal(2_800m, firstDay.Net);
            },
            thirdDay =>
            {
                Assert.Equal(new DateOnly(2026, 8, 3), thirdDay.Date);
                Assert.Equal(0m, thirdDay.Income);
                Assert.Equal(150m, thirdDay.Expense);
                Assert.Equal(-150m, thirdDay.Net);
            });

        Assert.Equal(1, readService.CallCount);
        Assert.Equal(userId, readService.LastUserId);
        Assert.Equal(new DateOnly(2026, 8, 1), readService.LastStartDate);
        Assert.Equal(new DateOnly(2026, 9, 1), readService.LastEndDateExclusive);
        Assert.DoesNotContain(
            typeof(DashboardModel).GetProperties(),
            property => property.Name == "UserId");
    }

    [Fact]
    public async Task Execute_WhenBudgetIsExceeded_PreservesNegativeRemainingAmount()
    {
        var readService = new FakeDashboardReadService
        {
            Data = new DashboardReadData(0m, 1_200m, 1, 1_000m, [], [])
        };
        var operation = new GetDashboard(
            new StubCurrentUser(Guid.NewGuid()),
            readService);

        var result = await operation.ExecuteAsync(8, 2026);

        var budget = Assert.IsType<DashboardBudgetModel>(result.Dashboard!.Budget);
        Assert.Equal(1_200m, budget.Spent);
        Assert.Equal(-200m, budget.Remaining);
        Assert.Equal(120m, budget.PercentageUsed);
        Assert.True(budget.IsExceeded);
    }

    [Fact]
    public async Task Execute_WithEmptyPeriod_ReturnsZerosNullBudgetAndEmptyCollections()
    {
        var operation = new GetDashboard(
            new StubCurrentUser(Guid.NewGuid()),
            new FakeDashboardReadService());

        var result = await operation.ExecuteAsync(8, 2026);

        Assert.Equal(GetDashboardStatus.Success, result.Status);
        var dashboard = result.Dashboard!;
        Assert.Equal(0m, dashboard.Summary.TotalIncome);
        Assert.Equal(0m, dashboard.Summary.TotalExpenses);
        Assert.Equal(0m, dashboard.Summary.Balance);
        Assert.Equal(0, dashboard.Summary.TransactionCount);
        Assert.Null(dashboard.Budget);
        Assert.Empty(dashboard.ExpensesByCategory);
        Assert.Empty(dashboard.DailyFlow);
    }

    [Fact]
    public async Task Execute_WithIncomeOnly_DoesNotCreateExpenseCategoryBreakdown()
    {
        var readService = new FakeDashboardReadService
        {
            Data = new DashboardReadData(
                500m,
                0m,
                1,
                null,
                [new(Guid.NewGuid(), "Unexpected", 10m)],
                [])
        };
        var operation = new GetDashboard(
            new StubCurrentUser(Guid.NewGuid()),
            readService);

        var result = await operation.ExecuteAsync(8, 2026);

        Assert.Empty(result.Dashboard!.ExpensesByCategory);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticatedWithoutReadingData()
    {
        var readService = new FakeDashboardReadService();
        var operation = new GetDashboard(
            new StubCurrentUser(null),
            readService);

        var result = await operation.ExecuteAsync(8, 2026);

        Assert.Equal(GetDashboardStatus.Unauthenticated, result.Status);
        Assert.Null(result.Dashboard);
        Assert.Equal(0, readService.CallCount);
    }

    [Theory]
    [InlineData(0, 2026)]
    [InlineData(13, 2026)]
    [InlineData(8, 0)]
    public async Task Execute_WithInvalidPeriod_ReturnsControlledErrorWithoutReadingData(
        int month,
        int year)
    {
        var readService = new FakeDashboardReadService();
        var operation = new GetDashboard(
            new StubCurrentUser(Guid.NewGuid()),
            readService);

        var result = await operation.ExecuteAsync(month, year);

        Assert.Equal(GetDashboardStatus.InvalidPeriod, result.Status);
        Assert.Null(result.Dashboard);
        Assert.Equal(["Dashboard period is invalid."], result.Errors);
        Assert.Equal(0, readService.CallCount);
    }

    [Fact]
    public async Task Execute_ForDecember_UsesNextJanuaryAsExclusiveEndDate()
    {
        var readService = new FakeDashboardReadService();
        var operation = new GetDashboard(
            new StubCurrentUser(Guid.NewGuid()),
            readService);

        await operation.ExecuteAsync(12, 2026);

        Assert.Equal(new DateOnly(2026, 12, 1), readService.LastStartDate);
        Assert.Equal(new DateOnly(2027, 1, 1), readService.LastEndDateExclusive);
    }
}

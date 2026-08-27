using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;
using SmartExpense.Infrastructure.Dashboard;
using SmartExpense.Tests.Integration.Persistence;

namespace SmartExpense.Tests.Integration.Dashboard;

[Collection(PostgreSqlCollection.Name)]
public sealed class DashboardReadServiceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetMonthlyAsync_AggregatesOnlyRequestedUsersSelectedPeriod()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var incomeCategory = CreateCategory(userId, "Salary", TransactionType.Income);
        var groceriesCategory = CreateCategory(userId, "Groceries", TransactionType.Expense);
        var transportCategory = CreateCategory(userId, "Transport", TransactionType.Expense);
        var otherUserCategory = CreateCategory(
            otherUserId,
            "Other user expense",
            TransactionType.Expense);

        await using (var seedContext = fixture.CreateDbContext())
        {
            seedContext.Categories.AddRange(
                incomeCategory,
                groceriesCategory,
                transportCategory,
                otherUserCategory);
            seedContext.Transactions.AddRange(
                CreateTransaction(userId, incomeCategory.Id, 3_000m, TransactionType.Income, new DateOnly(2026, 8, 1)),
                CreateTransaction(userId, groceriesCategory.Id, 200m, TransactionType.Expense, new DateOnly(2026, 8, 1)),
                CreateTransaction(userId, transportCategory.Id, 150m, TransactionType.Expense, new DateOnly(2026, 8, 3)),
                CreateTransaction(userId, groceriesCategory.Id, 900m, TransactionType.Expense, new DateOnly(2026, 7, 31)),
                CreateTransaction(userId, groceriesCategory.Id, 800m, TransactionType.Expense, new DateOnly(2026, 9, 1)),
                CreateTransaction(otherUserId, otherUserCategory.Id, 7_000m, TransactionType.Expense, new DateOnly(2026, 8, 2)));
            seedContext.Budgets.AddRange(
                CreateBudget(userId, 8, 2026, 1_000m),
                CreateBudget(userId, 7, 2026, 2_000m),
                CreateBudget(otherUserId, 8, 2026, 9_000m));

            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = fixture.CreateDbContext();
        var result = await new DashboardReadService(dbContext).GetMonthlyAsync(
            userId,
            8,
            2026,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 9, 1));

        Assert.Equal(3_000m, result.TotalIncome);
        Assert.Equal(350m, result.TotalExpenses);
        Assert.Equal(3, result.TransactionCount);
        Assert.Equal(1_000m, result.BudgetAmount);
        Assert.Collection(
            result.ExpensesByCategory,
            groceries =>
            {
                Assert.Equal(groceriesCategory.Id, groceries.CategoryId);
                Assert.Equal("Groceries", groceries.CategoryName);
                Assert.Equal(200m, groceries.Amount);
            },
            transport =>
            {
                Assert.Equal(transportCategory.Id, transport.CategoryId);
                Assert.Equal("Transport", transport.CategoryName);
                Assert.Equal(150m, transport.Amount);
            });
        Assert.Collection(
            result.DailyFlow,
            firstDay =>
            {
                Assert.Equal(new DateOnly(2026, 8, 1), firstDay.Date);
                Assert.Equal(3_000m, firstDay.Income);
                Assert.Equal(200m, firstDay.Expense);
            },
            thirdDay =>
            {
                Assert.Equal(new DateOnly(2026, 8, 3), thirdDay.Date);
                Assert.Equal(0m, thirdDay.Income);
                Assert.Equal(150m, thirdDay.Expense);
            });
    }

    private static Category CreateCategory(
        Guid userId,
        string name,
        TransactionType type)
    {
        return new Category(
            Guid.NewGuid(),
            name,
            type,
            userId,
            CreatedAt);
    }

    private static Transaction CreateTransaction(
        Guid userId,
        Guid categoryId,
        decimal amount,
        TransactionType type,
        DateOnly date)
    {
        return new Transaction(
            Guid.NewGuid(),
            "Dashboard transaction",
            amount,
            type,
            date,
            categoryId,
            userId,
            CreatedAt);
    }

    private static Budget CreateBudget(
        Guid userId,
        int month,
        int year,
        decimal amount)
    {
        return new Budget(
            Guid.NewGuid(),
            month,
            year,
            amount,
            userId,
            CreatedAt);
    }
}

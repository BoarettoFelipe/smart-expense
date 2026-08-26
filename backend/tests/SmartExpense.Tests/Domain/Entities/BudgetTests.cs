using SmartExpense.Domain.Entities;

namespace SmartExpense.Tests.Domain.Entities;

public class BudgetTests
{
    private static readonly Guid BudgetId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidValues_CreatesBudget()
    {
        var budget = CreateBudget();

        Assert.Equal(BudgetId, budget.Id);
        Assert.Equal(8, budget.Month);
        Assert.Equal(2026, budget.Year);
        Assert.Equal(500m, budget.Amount);
        Assert.Equal(UserId, budget.UserId);
        Assert.Equal(CreatedAt, budget.CreatedAt);
    }

    [Fact]
    public void Constructor_WithMonthBelowOne_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateBudget(month: 0));

        Assert.Equal("month", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithMonthAboveTwelve_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateBudget(month: 13));

        Assert.Equal("month", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithInvalidYear_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateBudget(year: 0));

        Assert.Equal("year", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateBudget(amount: 0));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateBudget(amount: -1));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateBudget(userId: Guid.Empty));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Update_WithValidValues_ChangesEditableFieldsAndPreservesIdentity()
    {
        var budget = CreateBudget();

        budget.Update(9, 2027, 750m);

        Assert.Equal(9, budget.Month);
        Assert.Equal(2027, budget.Year);
        Assert.Equal(750m, budget.Amount);
        Assert.Equal(BudgetId, budget.Id);
        Assert.Equal(UserId, budget.UserId);
        Assert.Equal(CreatedAt, budget.CreatedAt);
    }

    [Fact]
    public void Update_WithMonthBelowOne_ThrowsArgumentOutOfRangeException()
    {
        var budget = CreateBudget();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            budget.Update(0, 2027, 750m));

        Assert.Equal("month", exception.ParamName);
    }

    [Fact]
    public void Update_WithMonthAboveTwelve_ThrowsArgumentOutOfRangeException()
    {
        var budget = CreateBudget();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            budget.Update(13, 2027, 750m));

        Assert.Equal("month", exception.ParamName);
    }

    [Fact]
    public void Update_WithInvalidYear_ThrowsArgumentOutOfRangeException()
    {
        var budget = CreateBudget();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            budget.Update(9, 0, 750m));

        Assert.Equal("year", exception.ParamName);
    }

    [Fact]
    public void Update_WithInvalidAmount_ThrowsArgumentOutOfRangeException()
    {
        var budget = CreateBudget();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            budget.Update(9, 2027, 0m));

        Assert.Equal("amount", exception.ParamName);
    }

    private static Budget CreateBudget(
        int month = 8,
        int year = 2026,
        decimal amount = 500m,
        Guid? userId = null)
    {
        return new Budget(BudgetId, month, year, amount, userId ?? UserId, CreatedAt);
    }
}

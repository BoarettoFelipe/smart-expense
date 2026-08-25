using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Tests.Domain.Entities;

public class TransactionTests
{
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly TransactionDate = new(2026, 8, 13);
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidValues_CreatesTransaction()
    {
        var transaction = CreateTransaction();

        Assert.Equal(TransactionId, transaction.Id);
        Assert.Equal("Groceries", transaction.Description);
        Assert.Equal(100m, transaction.Amount);
        Assert.Equal(TransactionType.Expense, transaction.Type);
        Assert.Equal(TransactionDate, transaction.Date);
        Assert.Equal(CategoryId, transaction.CategoryId);
        Assert.Equal(UserId, transaction.UserId);
        Assert.Equal(CreatedAt, transaction.CreatedAt);
        Assert.Null(transaction.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithNullDescription_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateTransaction(description: null!));

        Assert.Equal("description", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyDescription_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateTransaction(description: string.Empty));

        Assert.Equal("description", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithWhitespaceDescription_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateTransaction(description: "   "));

        Assert.Equal("description", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateTransaction(amount: 0));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateTransaction(amount: -1));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyCategoryId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateTransaction(categoryId: Guid.Empty));

        Assert.Equal("categoryId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateTransaction(userId: Guid.Empty));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Update_WithValidValues_ChangesEditableFieldsAndPreservesIdentity()
    {
        var transaction = CreateTransaction();
        var newCategoryId = Guid.NewGuid();
        var newDate = new DateOnly(2026, 8, 25);
        var updatedAt = new DateTimeOffset(
            2026,
            8,
            25,
            12,
            0,
            0,
            TimeSpan.Zero);

        transaction.Update(
            "Updated groceries",
            150m,
            TransactionType.Income,
            newDate,
            newCategoryId,
            updatedAt);

        Assert.Equal("Updated groceries", transaction.Description);
        Assert.Equal(150m, transaction.Amount);
        Assert.Equal(TransactionType.Income, transaction.Type);
        Assert.Equal(newDate, transaction.Date);
        Assert.Equal(newCategoryId, transaction.CategoryId);
        Assert.Equal(updatedAt, transaction.UpdatedAt);
        Assert.Equal(TransactionId, transaction.Id);
        Assert.Equal(UserId, transaction.UserId);
        Assert.Equal(CreatedAt, transaction.CreatedAt);
    }

    [Fact]
    public void Update_WithWhitespaceDescription_ThrowsArgumentException()
    {
        var transaction = CreateTransaction();

        var exception = Assert.Throws<ArgumentException>(() => transaction.Update(
            "   ",
            150m,
            TransactionType.Expense,
            TransactionDate,
            CategoryId,
            DateTimeOffset.UtcNow));

        Assert.Equal("description", exception.ParamName);
    }

    [Fact]
    public void Update_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        var transaction = CreateTransaction();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => transaction.Update(
            "Groceries",
            0m,
            TransactionType.Expense,
            TransactionDate,
            CategoryId,
            DateTimeOffset.UtcNow));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Update_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var transaction = CreateTransaction();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => transaction.Update(
            "Groceries",
            -1m,
            TransactionType.Expense,
            TransactionDate,
            CategoryId,
            DateTimeOffset.UtcNow));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Update_WithEmptyCategoryId_ThrowsArgumentException()
    {
        var transaction = CreateTransaction();

        var exception = Assert.Throws<ArgumentException>(() => transaction.Update(
            "Groceries",
            150m,
            TransactionType.Expense,
            TransactionDate,
            Guid.Empty,
            DateTimeOffset.UtcNow));

        Assert.Equal("categoryId", exception.ParamName);
    }

    private static Transaction CreateTransaction(
        string description = "Groceries",
        decimal amount = 100m,
        Guid? categoryId = null,
        Guid? userId = null)
    {
        return new Transaction(
            TransactionId,
            description,
            amount,
            TransactionType.Expense,
            TransactionDate,
            categoryId ?? CategoryId,
            userId ?? UserId,
            CreatedAt);
    }
}

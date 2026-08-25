using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Tests.Domain.Entities;

public class CategoryTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidValues_CreatesCategory()
    {
        var category = CreateCategory();

        Assert.Equal(CategoryId, category.Id);
        Assert.Equal("Salary", category.Name);
        Assert.Equal(TransactionType.Income, category.Type);
        Assert.Equal(UserId, category.UserId);
        Assert.Equal(CreatedAt, category.CreatedAt);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateCategory(name: null!));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateCategory(name: string.Empty));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateCategory(name: "   "));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateCategory(userId: Guid.Empty));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Update_WithValidValues_ChangesEditableFieldsAndPreservesIdentity()
    {
        var category = CreateCategory();

        category.Update("Updated category", TransactionType.Expense);

        Assert.Equal("Updated category", category.Name);
        Assert.Equal(TransactionType.Expense, category.Type);
        Assert.Equal(CategoryId, category.Id);
        Assert.Equal(UserId, category.UserId);
        Assert.Equal(CreatedAt, category.CreatedAt);
    }

    [Fact]
    public void Update_WithNullName_ThrowsArgumentException()
    {
        var category = CreateCategory();

        var exception = Assert.Throws<ArgumentException>(() =>
            category.Update(null!, TransactionType.Expense));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        var category = CreateCategory();

        var exception = Assert.Throws<ArgumentException>(() =>
            category.Update(string.Empty, TransactionType.Expense));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Update_WithWhitespaceName_ThrowsArgumentException()
    {
        var category = CreateCategory();

        var exception = Assert.Throws<ArgumentException>(() =>
            category.Update("   ", TransactionType.Expense));

        Assert.Equal("name", exception.ParamName);
    }

    private static Category CreateCategory(string name = "Salary", Guid? userId = null)
    {
        return new Category(
            CategoryId,
            name,
            TransactionType.Income,
            userId ?? UserId,
            CreatedAt);
    }
}

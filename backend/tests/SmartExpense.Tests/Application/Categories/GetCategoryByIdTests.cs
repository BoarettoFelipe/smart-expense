using SmartExpense.Application.Categories;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Categories;

public sealed class GetCategoryByIdTests
{
    [Fact]
    public async Task Execute_WithOwnedCategory_ReturnsCategory()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);
        var repository = new FakeCategoryRepository();
        repository.Categories.Add(category);
        var operation = new GetCategoryById(
            new StubCurrentUser(userId),
            repository);

        var result = await operation.ExecuteAsync(category.Id);

        Assert.Equal(GetCategoryByIdStatus.Success, result.Status);
        Assert.Equal(category.Id, result.Category?.Id);
        Assert.Equal(category.Id, repository.LastQueriedCategoryId);
        Assert.Equal(userId, repository.LastQueriedUserId);
    }

    [Fact]
    public async Task Execute_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeCategoryRepository();
        var operation = new GetCategoryById(
            new StubCurrentUser(userId),
            repository);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(GetCategoryByIdStatus.NotFound, result.Status);
        Assert.Null(result.Category);
        Assert.Equal(userId, repository.LastQueriedUserId);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersCategory_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUsersCategory = CreateCategory(Guid.NewGuid());
        var repository = new FakeCategoryRepository();
        repository.Categories.Add(otherUsersCategory);
        var operation = new GetCategoryById(
            new StubCurrentUser(userId),
            repository);

        var result = await operation.ExecuteAsync(otherUsersCategory.Id);

        Assert.Equal(GetCategoryByIdStatus.NotFound, result.Status);
        Assert.Null(result.Category);
        Assert.Equal(userId, repository.LastQueriedUserId);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeCategoryRepository();
        var operation = new GetCategoryById(
            new StubCurrentUser(null),
            repository);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(GetCategoryByIdStatus.Unauthenticated, result.Status);
        Assert.Null(result.Category);
        Assert.Equal(0, repository.GetByIdCallCount);
    }

    private static Category CreateCategory(Guid userId)
    {
        return new Category(
            Guid.NewGuid(),
            $"Category-{Guid.NewGuid():N}",
            TransactionType.Expense,
            userId,
            DateTimeOffset.UtcNow);
    }
}

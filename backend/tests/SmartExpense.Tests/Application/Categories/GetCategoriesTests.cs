using SmartExpense.Application.Categories;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Categories;

public sealed class GetCategoriesTests
{
    [Fact]
    public async Task Execute_ReturnsOnlyCurrentUsersRepositoryResults()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeCategoryRepository();
        var expected = CreateCategory(userId);
        repository.Categories.Add(expected);
        repository.Categories.Add(CreateCategory(Guid.NewGuid()));
        var operation = new GetCategories(
            new StubCurrentUser(userId),
            repository);

        var result = await operation.ExecuteAsync();

        Assert.Equal(GetCategoriesStatus.Success, result.Status);
        var category = Assert.Single(result.Categories);
        Assert.Equal(expected.Id, category.Id);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Equal(1, repository.GetByUserCallCount);
        Assert.DoesNotContain(
            typeof(CategoryModel).GetProperties(),
            property => property.Name == "UserId");
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeCategoryRepository();
        var operation = new GetCategories(
            new StubCurrentUser(null),
            repository);

        var result = await operation.ExecuteAsync();

        Assert.Equal(GetCategoriesStatus.Unauthenticated, result.Status);
        Assert.Empty(result.Categories);
        Assert.Equal(0, repository.GetByUserCallCount);
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

using SmartExpense.Application.Categories;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Categories;

public sealed class UpdateCategoryTests
{
    [Fact]
    public async Task Execute_WithOwnedCategory_UpdatesAndPersists()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);
        var originalId = category.Id;
        var originalUserId = category.UserId;
        var originalCreatedAt = category.CreatedAt;
        var repository = new FakeCategoryRepository();
        repository.Categories.Add(category);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateCategory(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);
        var command = new UpdateCategoryCommand(
            category.Id,
            "Updated category",
            TransactionType.Income);

        var result = await operation.ExecuteAsync(command);

        Assert.Equal(UpdateCategoryStatus.Success, result.Status);
        Assert.NotNull(result.Category);
        Assert.Equal(command.Name, result.Category.Name);
        Assert.Equal(command.Type, result.Category.Type);
        Assert.Equal(originalId, result.Category.Id);
        Assert.Equal(originalUserId, category.UserId);
        Assert.Equal(originalCreatedAt, result.Category.CreatedAt);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void Command_DoesNotExposeClientControlledServerValues()
    {
        var properties = typeof(UpdateCategoryCommand).GetProperties();

        Assert.DoesNotContain(properties, property => property.Name == "UserId");
        Assert.DoesNotContain(properties, property => property.Name == "CreatedAt");
    }

    [Fact]
    public async Task Execute_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateCategory(
            new StubCurrentUser(Guid.NewGuid()),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateCategoryCommand(
            Guid.NewGuid(),
            "Updated category",
            TransactionType.Expense));

        Assert.Equal(UpdateCategoryStatus.NotFound, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersCategory_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUsersCategory = CreateCategory(Guid.NewGuid());
        var repository = new FakeCategoryRepository();
        repository.Categories.Add(otherUsersCategory);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateCategory(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateCategoryCommand(
            otherUsersCategory.Id,
            "Updated category",
            TransactionType.Expense));

        Assert.Equal(UpdateCategoryStatus.NotFound, result.Status);
        Assert.Equal(userId, repository.LastQueriedUserId);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithInvalidDomainData_DoesNotPersist()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);
        var repository = new FakeCategoryRepository();
        repository.Categories.Add(category);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateCategory(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateCategoryCommand(
            category.Id,
            "   ",
            TransactionType.Expense));

        Assert.Equal(UpdateCategoryStatus.Invalid, result.Status);
        Assert.Equal(["Category data is invalid."], result.Errors);
        Assert.Equal("Original category", category.Name);
        Assert.Equal(TransactionType.Income, category.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateCategory(
            new StubCurrentUser(null),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(new UpdateCategoryCommand(
            Guid.NewGuid(),
            "Updated category",
            TransactionType.Expense));

        Assert.Equal(UpdateCategoryStatus.Unauthenticated, result.Status);
        Assert.Equal(0, repository.GetByIdCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static Category CreateCategory(Guid userId)
    {
        return new Category(
            Guid.NewGuid(),
            "Original category",
            TransactionType.Income,
            userId,
            new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));
    }
}

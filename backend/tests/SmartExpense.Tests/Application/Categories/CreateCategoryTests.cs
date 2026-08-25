using SmartExpense.Application.Categories;
using SmartExpense.Domain.Enums;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Categories;

public sealed class CreateCategoryTests
{
    [Fact]
    public async Task Execute_WithValidData_CreatesForCurrentUserAndPersists()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateCategory(
            new StubCurrentUser(userId),
            repository,
            unitOfWork);
        var beforeCreation = DateTimeOffset.UtcNow;

        var result = await operation.ExecuteAsync(
            new CreateCategoryCommand("Salary", TransactionType.Income));

        var afterCreation = DateTimeOffset.UtcNow;
        Assert.Equal(CreateCategoryStatus.Success, result.Status);
        Assert.NotNull(result.Category);
        Assert.NotEqual(Guid.Empty, result.Category.Id);
        Assert.Equal("Salary", result.Category.Name);
        Assert.Equal(TransactionType.Income, result.Category.Type);
        Assert.InRange(result.Category.CreatedAt, beforeCreation, afterCreation);
        var persisted = Assert.Single(repository.AddedCategories);
        Assert.Equal(userId, persisted.UserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void Command_DoesNotExposeClientControlledUserId()
    {
        Assert.DoesNotContain(
            typeof(CreateCategoryCommand).GetProperties(),
            property => property.Name == "UserId");
    }

    [Fact]
    public async Task Execute_WithInvalidDomainData_ReturnsStableInvalidResult()
    {
        var repository = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateCategory(
            new StubCurrentUser(Guid.NewGuid()),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(
            new CreateCategoryCommand("   ", TransactionType.Expense));

        Assert.Equal(CreateCategoryStatus.Invalid, result.Status);
        Assert.Equal(["Category data is invalid."], result.Errors);
        Assert.Empty(repository.AddedCategories);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var repository = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new CreateCategory(
            new StubCurrentUser(null),
            repository,
            unitOfWork);

        var result = await operation.ExecuteAsync(
            new CreateCategoryCommand("Salary", TransactionType.Income));

        Assert.Equal(CreateCategoryStatus.Unauthenticated, result.Status);
        Assert.Empty(repository.AddedCategories);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

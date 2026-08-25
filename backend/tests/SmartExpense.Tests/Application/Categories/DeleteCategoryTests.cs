using SmartExpense.Application.Categories;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;
using SmartExpense.Tests.Application.Transactions;

namespace SmartExpense.Tests.Application.Categories;

public sealed class DeleteCategoryTests
{
    [Fact]
    public async Task Execute_WithUnusedOwnedCategory_RemovesAndPersists()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(category);
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteCategory(
            new StubCurrentUser(userId),
            categoryRepository,
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(category.Id);

        Assert.Equal(DeleteCategoryStatus.Success, result.Status);
        Assert.Same(category, Assert.Single(categoryRepository.RemovedCategories));
        Assert.Empty(categoryRepository.Categories);
        Assert.Equal(category.Id, transactionRepository.LastQueriedCategoryId);
        Assert.Equal(userId, transactionRepository.LastQueriedUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithCategoryInUse_ReturnsConflictWithoutRemoving()
    {
        var userId = Guid.NewGuid();
        var category = CreateCategory(userId);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(category);
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(CreateTransaction(
            category.Id,
            userId));
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteCategory(
            new StubCurrentUser(userId),
            categoryRepository,
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(category.Id);

        Assert.Equal(DeleteCategoryStatus.CategoryInUse, result.Status);
        Assert.Empty(categoryRepository.RemovedCategories);
        Assert.Contains(category, categoryRepository.Categories);
        Assert.Equal(1, transactionRepository.ExistsByCategoryCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersCategory_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUsersCategory = CreateCategory(Guid.NewGuid());
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(otherUsersCategory);
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteCategory(
            new StubCurrentUser(userId),
            categoryRepository,
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(otherUsersCategory.Id);

        Assert.Equal(DeleteCategoryStatus.NotFound, result.Status);
        Assert.Equal(userId, categoryRepository.LastQueriedUserId);
        Assert.Equal(0, transactionRepository.ExistsByCategoryCallCount);
        Assert.Empty(categoryRepository.RemovedCategories);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var categoryRepository = new FakeCategoryRepository();
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteCategory(
            new StubCurrentUser(Guid.NewGuid()),
            categoryRepository,
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(DeleteCategoryStatus.NotFound, result.Status);
        Assert.Equal(0, transactionRepository.ExistsByCategoryCallCount);
        Assert.Empty(categoryRepository.RemovedCategories);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var categoryRepository = new FakeCategoryRepository();
        var transactionRepository = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new DeleteCategory(
            new StubCurrentUser(null),
            categoryRepository,
            transactionRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(DeleteCategoryStatus.Unauthenticated, result.Status);
        Assert.Equal(0, categoryRepository.GetByIdCallCount);
        Assert.Equal(0, transactionRepository.ExistsByCategoryCallCount);
        Assert.Empty(categoryRepository.RemovedCategories);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static Category CreateCategory(Guid userId)
    {
        return new Category(
            Guid.NewGuid(),
            "Category",
            TransactionType.Expense,
            userId,
            DateTimeOffset.UtcNow);
    }

    private static Transaction CreateTransaction(Guid categoryId, Guid userId)
    {
        return new Transaction(
            Guid.NewGuid(),
            "Transaction",
            100m,
            TransactionType.Expense,
            new DateOnly(2026, 8, 25),
            categoryId,
            userId,
            DateTimeOffset.UtcNow);
    }
}

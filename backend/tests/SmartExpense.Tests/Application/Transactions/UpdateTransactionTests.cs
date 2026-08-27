using SmartExpense.Application.Transactions;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Tests.Application.Transactions;

public sealed class UpdateTransactionTests
{
    [Fact]
    public async Task Execute_WithOwnedTransactionAndCategory_UpdatesAndPersists()
    {
        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId);
        var originalId = transaction.Id;
        var originalUserId = transaction.UserId;
        var originalCreatedAt = transaction.CreatedAt;
        var category = CreateCategory(userId);
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(transaction);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(category);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            categoryRepository,
            unitOfWork);
        var command = CreateCommand(transaction.Id, category.Id);
        var beforeUpdate = DateTimeOffset.UtcNow;

        var result = await operation.ExecuteAsync(command);

        var afterUpdate = DateTimeOffset.UtcNow;
        Assert.Equal(UpdateTransactionStatus.Success, result.Status);
        Assert.NotNull(result.Transaction);
        Assert.Equal(command.Description, result.Transaction.Description);
        Assert.Equal(command.Amount, result.Transaction.Amount);
        Assert.Equal(command.Type, result.Transaction.Type);
        Assert.Equal(command.Date, result.Transaction.Date);
        Assert.Equal(command.CategoryId, result.Transaction.CategoryId);
        Assert.NotNull(result.Transaction.UpdatedAt);
        Assert.InRange(result.Transaction.UpdatedAt.Value, beforeUpdate, afterUpdate);
        Assert.Equal(TimeSpan.Zero, result.Transaction.UpdatedAt.Value.Offset);
        Assert.Equal(originalId, result.Transaction.Id);
        Assert.Equal(originalUserId, transaction.UserId);
        Assert.Equal(originalCreatedAt, result.Transaction.CreatedAt);
        Assert.Equal(userId, transactionRepository.LastQueriedUserId);
        Assert.Equal(transaction.Id, transactionRepository.LastQueriedTransactionId);
        Assert.Equal(userId, categoryRepository.LastQueriedUserId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithMatchingExpenseCategory_UpdatesAndPersists()
    {
        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId);
        var category = CreateCategory(userId, TransactionType.Expense);
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(transaction);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(category);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            categoryRepository,
            unitOfWork);
        var command = CreateCommand(transaction.Id, category.Id) with
        {
            Type = TransactionType.Expense
        };

        var result = await operation.ExecuteAsync(command);

        Assert.Equal(UpdateTransactionStatus.Success, result.Status);
        Assert.Equal(TransactionType.Expense, result.Transaction!.Type);
        Assert.Equal(category.Id, transaction.CategoryId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithMismatchedCategoryType_ReturnsCategoryTypeMismatchWithoutUpdatingOrPersisting()
    {
        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId);
        var category = CreateCategory(userId, TransactionType.Expense);
        var originalDescription = transaction.Description;
        var originalCategoryId = transaction.CategoryId;
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(transaction);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(category);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            categoryRepository,
            unitOfWork);
        var command = CreateCommand(transaction.Id, category.Id);

        var result = await operation.ExecuteAsync(command);

        Assert.Equal(UpdateTransactionStatus.CategoryTypeMismatch, result.Status);
        Assert.Equal(
            ["Transaction type must match the selected category type."],
            result.Errors);
        Assert.Equal(originalDescription, transaction.Description);
        Assert.Equal(originalCategoryId, transaction.CategoryId);
        Assert.Null(transaction.UpdatedAt);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void Command_DoesNotExposeClientControlledServerValues()
    {
        var properties = typeof(UpdateTransactionCommand).GetProperties();

        Assert.DoesNotContain(properties, property => property.Name == "UserId");
        Assert.DoesNotContain(properties, property => property.Name == "CreatedAt");
        Assert.DoesNotContain(properties, property => property.Name == "UpdatedAt");
    }

    [Fact]
    public async Task Execute_WithUnavailableCategory_DoesNotUpdateOrPersist()
    {
        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId);
        var originalDescription = transaction.Description;
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(transaction);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            new FakeCategoryRepository(),
            unitOfWork);

        var result = await operation.ExecuteAsync(
            CreateCommand(transaction.Id, Guid.NewGuid()));

        Assert.Equal(UpdateTransactionStatus.CategoryUnavailable, result.Status);
        Assert.Equal(originalDescription, transaction.Description);
        Assert.Null(transaction.UpdatedAt);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersCategory_ReturnsCategoryUnavailable()
    {
        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId);
        var otherUsersCategory = CreateCategory(Guid.NewGuid());
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(transaction);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(otherUsersCategory);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            categoryRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(
            CreateCommand(transaction.Id, otherUsersCategory.Id));

        Assert.Equal(UpdateTransactionStatus.CategoryUnavailable, result.Status);
        Assert.Equal(userId, categoryRepository.LastQueriedUserId);
        Assert.Null(transaction.UpdatedAt);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var categoryRepository = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(userId),
            new FakeTransactionRepository(),
            categoryRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(
            CreateCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(UpdateTransactionStatus.NotFound, result.Status);
        Assert.Equal(0, categoryRepository.GetByIdCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithAnotherUsersTransaction_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUsersTransaction = CreateTransaction(Guid.NewGuid());
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(otherUsersTransaction);
        var categoryRepository = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            categoryRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(
            CreateCommand(otherUsersTransaction.Id, Guid.NewGuid()));

        Assert.Equal(UpdateTransactionStatus.NotFound, result.Status);
        Assert.Equal(userId, transactionRepository.LastQueriedUserId);
        Assert.Equal(0, categoryRepository.GetByIdCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithoutCurrentUser_ReturnsUnauthenticated()
    {
        var transactionRepository = new FakeTransactionRepository();
        var categoryRepository = new FakeCategoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(null),
            transactionRepository,
            categoryRepository,
            unitOfWork);

        var result = await operation.ExecuteAsync(
            CreateCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(UpdateTransactionStatus.Unauthenticated, result.Status);
        Assert.Equal(0, transactionRepository.GetByIdCallCount);
        Assert.Equal(0, categoryRepository.GetByIdCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Execute_WithInvalidDomainData_ReturnsStableInvalidResult()
    {
        var userId = Guid.NewGuid();
        var transaction = CreateTransaction(userId);
        var category = CreateCategory(userId);
        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(transaction);
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Categories.Add(category);
        var unitOfWork = new FakeUnitOfWork();
        var operation = new UpdateTransaction(
            new StubCurrentUser(userId),
            transactionRepository,
            categoryRepository,
            unitOfWork);
        var command = CreateCommand(transaction.Id, category.Id) with
        {
            Amount = 0m
        };

        var result = await operation.ExecuteAsync(command);

        Assert.Equal(UpdateTransactionStatus.Invalid, result.Status);
        Assert.Equal(["Transaction data is invalid."], result.Errors);
        Assert.Null(transaction.UpdatedAt);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static UpdateTransactionCommand CreateCommand(
        Guid transactionId,
        Guid categoryId)
    {
        return new UpdateTransactionCommand(
            transactionId,
            "Updated transaction",
            250m,
            TransactionType.Income,
            new DateOnly(2026, 8, 25),
            categoryId);
    }

    private static Transaction CreateTransaction(Guid userId)
    {
        return new Transaction(
            Guid.NewGuid(),
            "Original transaction",
            100m,
            TransactionType.Expense,
            new DateOnly(2026, 8, 19),
            Guid.NewGuid(),
            userId,
            new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero));
    }

    private static Category CreateCategory(
        Guid userId,
        TransactionType type = TransactionType.Income)
    {
        return new Category(
            Guid.NewGuid(),
            $"Category-{Guid.NewGuid():N}",
            type,
            userId,
            DateTimeOffset.UtcNow);
    }
}

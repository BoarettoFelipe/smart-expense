using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Application.Transactions;

public sealed class CreateTransaction(
    ICurrentUser currentUser,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<CreateTransactionResult> ExecuteAsync(
        CreateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return CreateTransactionResult.Unauthenticated();
        }

        var category = await categoryRepository.GetByIdAsync(
            command.CategoryId,
            userId,
            cancellationToken);

        if (category is null)
        {
            return CreateTransactionResult.CategoryUnavailable();
        }

        if (category.Type != command.Type)
        {
            return CreateTransactionResult.CategoryTypeMismatch();
        }

        Transaction transaction;

        try
        {
            transaction = new Transaction(
                Guid.NewGuid(),
                command.Description,
                command.Amount,
                command.Type,
                command.Date,
                command.CategoryId,
                userId,
                DateTimeOffset.UtcNow);
        }
        catch (ArgumentException)
        {
            return CreateTransactionResult.Invalid();
        }

        await transactionRepository.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateTransactionResult.Success(
            TransactionModel.FromEntity(transaction));
    }
}

public sealed record CreateTransactionCommand(
    string Description,
    decimal Amount,
    TransactionType Type,
    DateOnly Date,
    Guid CategoryId);

public enum CreateTransactionStatus
{
    Success,
    Unauthenticated,
    CategoryUnavailable,
    CategoryTypeMismatch,
    Invalid
}

public sealed record CreateTransactionResult(
    CreateTransactionStatus Status,
    TransactionModel? Transaction,
    IReadOnlyCollection<string> Errors)
{
    public static CreateTransactionResult Success(TransactionModel transaction) =>
        new(CreateTransactionStatus.Success, transaction, []);

    public static CreateTransactionResult Unauthenticated() =>
        new(CreateTransactionStatus.Unauthenticated, null, []);

    public static CreateTransactionResult CategoryUnavailable() =>
        new(CreateTransactionStatus.CategoryUnavailable, null, []);

    public static CreateTransactionResult CategoryTypeMismatch() =>
        new(
            CreateTransactionStatus.CategoryTypeMismatch,
            null,
            ["Transaction type must match the selected category type."]);

    public static CreateTransactionResult Invalid() =>
        new(
            CreateTransactionStatus.Invalid,
            null,
            ["Transaction data is invalid."]);
}

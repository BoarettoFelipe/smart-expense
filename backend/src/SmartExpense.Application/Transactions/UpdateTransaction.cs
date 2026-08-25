using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Application.Transactions;

public sealed class UpdateTransaction(
    ICurrentUser currentUser,
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<UpdateTransactionResult> ExecuteAsync(
        UpdateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return UpdateTransactionResult.Unauthenticated();
        }

        var transaction = await transactionRepository.GetByIdAsync(
            command.Id,
            userId,
            cancellationToken);

        if (transaction is null)
        {
            return UpdateTransactionResult.NotFound();
        }

        var category = await categoryRepository.GetByIdAsync(
            command.CategoryId,
            userId,
            cancellationToken);

        if (category is null)
        {
            return UpdateTransactionResult.CategoryUnavailable();
        }

        try
        {
            transaction.Update(
                command.Description,
                command.Amount,
                command.Type,
                command.Date,
                command.CategoryId,
                DateTimeOffset.UtcNow);
        }
        catch (ArgumentException)
        {
            return UpdateTransactionResult.Invalid();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateTransactionResult.Success(
            TransactionModel.FromEntity(transaction));
    }
}

public sealed record UpdateTransactionCommand(
    Guid Id,
    string Description,
    decimal Amount,
    TransactionType Type,
    DateOnly Date,
    Guid CategoryId);

public enum UpdateTransactionStatus
{
    Success,
    Unauthenticated,
    NotFound,
    CategoryUnavailable,
    Invalid
}

public sealed record UpdateTransactionResult(
    UpdateTransactionStatus Status,
    TransactionModel? Transaction,
    IReadOnlyCollection<string> Errors)
{
    public static UpdateTransactionResult Success(TransactionModel transaction) =>
        new(UpdateTransactionStatus.Success, transaction, []);

    public static UpdateTransactionResult Unauthenticated() =>
        new(UpdateTransactionStatus.Unauthenticated, null, []);

    public static UpdateTransactionResult NotFound() =>
        new(UpdateTransactionStatus.NotFound, null, []);

    public static UpdateTransactionResult CategoryUnavailable() =>
        new(UpdateTransactionStatus.CategoryUnavailable, null, []);

    public static UpdateTransactionResult Invalid() =>
        new(
            UpdateTransactionStatus.Invalid,
            null,
            ["Transaction data is invalid."]);
}

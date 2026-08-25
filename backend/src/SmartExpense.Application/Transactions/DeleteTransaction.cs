using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Transactions;

public sealed class DeleteTransaction(
    ICurrentUser currentUser,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<DeleteTransactionResult> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return DeleteTransactionResult.Unauthenticated();
        }

        var transaction = await transactionRepository.GetByIdAsync(
            id,
            userId,
            cancellationToken);

        if (transaction is null)
        {
            return DeleteTransactionResult.NotFound();
        }

        transactionRepository.Remove(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DeleteTransactionResult.Success();
    }
}

public enum DeleteTransactionStatus
{
    Success,
    Unauthenticated,
    NotFound
}

public sealed record DeleteTransactionResult(DeleteTransactionStatus Status)
{
    public static DeleteTransactionResult Success() =>
        new(DeleteTransactionStatus.Success);

    public static DeleteTransactionResult Unauthenticated() =>
        new(DeleteTransactionStatus.Unauthenticated);

    public static DeleteTransactionResult NotFound() =>
        new(DeleteTransactionStatus.NotFound);
}

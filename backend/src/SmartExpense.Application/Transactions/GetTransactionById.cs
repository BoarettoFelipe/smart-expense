using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Transactions;

public sealed class GetTransactionById(
    ICurrentUser currentUser,
    ITransactionRepository transactionRepository)
{
    public async Task<GetTransactionByIdResult> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return GetTransactionByIdResult.Unauthenticated();
        }

        var transaction = await transactionRepository.GetByIdAsync(
            id,
            userId,
            cancellationToken);

        return transaction is null
            ? GetTransactionByIdResult.NotFound()
            : GetTransactionByIdResult.Success(
                TransactionModel.FromEntity(transaction));
    }
}

public enum GetTransactionByIdStatus
{
    Success,
    Unauthenticated,
    NotFound
}

public sealed record GetTransactionByIdResult(
    GetTransactionByIdStatus Status,
    TransactionModel? Transaction)
{
    public static GetTransactionByIdResult Success(TransactionModel transaction) =>
        new(GetTransactionByIdStatus.Success, transaction);

    public static GetTransactionByIdResult Unauthenticated() =>
        new(GetTransactionByIdStatus.Unauthenticated, null);

    public static GetTransactionByIdResult NotFound() =>
        new(GetTransactionByIdStatus.NotFound, null);
}

using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Persistence;

namespace SmartExpense.Application.Transactions;

public sealed class GetTransactions(
    ICurrentUser currentUser,
    ITransactionRepository transactionRepository)
{
    public async Task<GetTransactionsResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return GetTransactionsResult.Unauthenticated();
        }

        var transactions = await transactionRepository.GetByUserAsync(
            userId,
            cancellationToken);

        return GetTransactionsResult.Success(
            transactions.Select(TransactionModel.FromEntity).ToArray());
    }
}

public enum GetTransactionsStatus
{
    Success,
    Unauthenticated
}

public sealed record GetTransactionsResult(
    GetTransactionsStatus Status,
    IReadOnlyList<TransactionModel> Transactions)
{
    public static GetTransactionsResult Success(
        IReadOnlyList<TransactionModel> transactions) =>
        new(GetTransactionsStatus.Success, transactions);

    public static GetTransactionsResult Unauthenticated() =>
        new(GetTransactionsStatus.Unauthenticated, []);
}

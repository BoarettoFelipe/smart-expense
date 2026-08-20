using SmartExpense.Domain.Entities;
using SmartExpense.Domain.Enums;

namespace SmartExpense.Application.Transactions;

public sealed record TransactionModel(
    Guid Id,
    string Description,
    decimal Amount,
    TransactionType Type,
    DateOnly Date,
    Guid CategoryId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    internal static TransactionModel FromEntity(Transaction transaction)
    {
        return new TransactionModel(
            transaction.Id,
            transaction.Description,
            transaction.Amount,
            transaction.Type,
            transaction.Date,
            transaction.CategoryId,
            transaction.CreatedAt,
            transaction.UpdatedAt);
    }
}

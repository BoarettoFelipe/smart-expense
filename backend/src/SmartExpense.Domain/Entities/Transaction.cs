using SmartExpense.Domain.Enums;

namespace SmartExpense.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public DateOnly Date { get; set; }

    public Guid CategoryId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

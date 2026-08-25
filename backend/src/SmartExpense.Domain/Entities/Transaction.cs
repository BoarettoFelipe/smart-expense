using SmartExpense.Domain.Enums;

namespace SmartExpense.Domain.Entities;

public class Transaction
{
    public Transaction(
        Guid id,
        string description,
        decimal amount,
        TransactionType type,
        DateOnly date,
        Guid categoryId,
        Guid userId,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt = null)
    {
        ValidateEditableFields(description, amount, categoryId);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID must not be empty.", nameof(userId));
        }

        Id = id;
        Description = description;
        Amount = amount;
        Type = type;
        Date = date;
        CategoryId = categoryId;
        UserId = userId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void Update(
        string description,
        decimal amount,
        TransactionType type,
        DateOnly date,
        Guid categoryId,
        DateTimeOffset updatedAt)
    {
        ValidateEditableFields(description, amount, categoryId);

        Description = description;
        Amount = amount;
        Type = type;
        Date = date;
        CategoryId = categoryId;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }

    public TransactionType Type { get; private set; }

    public DateOnly Date { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private static void ValidateEditableFields(
        string description,
        decimal amount,
        Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Description must not be empty or whitespace.",
                nameof(description));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount must be greater than zero.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID must not be empty.",
                nameof(categoryId));
        }
    }
}

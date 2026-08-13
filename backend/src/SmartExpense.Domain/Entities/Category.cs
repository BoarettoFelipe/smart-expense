using SmartExpense.Domain.Enums;

namespace SmartExpense.Domain.Entities;

public class Category
{
    public Category(
        Guid id,
        string name,
        TransactionType type,
        Guid userId,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty or whitespace.", nameof(name));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID must not be empty.", nameof(userId));
        }

        Id = id;
        Name = name;
        Type = type;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public TransactionType Type { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

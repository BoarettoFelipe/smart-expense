namespace SmartExpense.Domain.Entities;

public class Budget
{
    public Budget(
        Guid id,
        int month,
        int year,
        decimal amount,
        Guid userId,
        DateTimeOffset createdAt)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }

        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than zero.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID must not be empty.", nameof(userId));
        }

        Id = id;
        Month = month;
        Year = year;
        Amount = amount;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public int Month { get; private set; }

    public int Year { get; private set; }

    public decimal Amount { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}

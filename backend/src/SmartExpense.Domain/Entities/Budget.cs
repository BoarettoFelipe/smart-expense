namespace SmartExpense.Domain.Entities;

public class Budget
{
    public Guid Id { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal Amount { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

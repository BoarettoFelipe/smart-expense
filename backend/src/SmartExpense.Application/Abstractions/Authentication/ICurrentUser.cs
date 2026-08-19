namespace SmartExpense.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    Guid? UserId { get; }
}

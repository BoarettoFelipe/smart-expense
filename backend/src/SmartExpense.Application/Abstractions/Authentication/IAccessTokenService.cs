namespace SmartExpense.Application.Abstractions.Authentication;

public interface IAccessTokenService
{
    AccessToken Create(UserAccount user);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

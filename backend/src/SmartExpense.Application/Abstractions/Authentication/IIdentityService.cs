namespace SmartExpense.Application.Abstractions.Authentication;

public interface IIdentityService
{
    Task<UserCreationResult> CreateUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> CheckPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record UserAccount(Guid Id, string Email);

public sealed record UserCreationError(string Code, string Description);

public sealed record UserCreationResult(
    bool Succeeded,
    Guid? UserId,
    IReadOnlyCollection<UserCreationError> Errors);

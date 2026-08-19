using SmartExpense.Application.Abstractions.Authentication;

namespace SmartExpense.Application.Authentication;

public sealed class RegisterUser(IIdentityService identityService)
{
    public async Task<RegisterUserResult> ExecuteAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<UserCreationError>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add(new UserCreationError("EmailRequired", "Email is required."));
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add(new UserCreationError("PasswordRequired", "Password is required."));
        }

        if (errors.Count > 0)
        {
            return new RegisterUserResult(false, null, errors);
        }

        var creationResult = await identityService.CreateUserAsync(
            command.Email,
            command.Password,
            cancellationToken);

        return new RegisterUserResult(
            creationResult.Succeeded,
            creationResult.UserId,
            creationResult.Errors);
    }
}

public sealed record RegisterUserCommand(string Email, string Password);

public sealed record RegisterUserResult(
    bool Succeeded,
    Guid? UserId,
    IReadOnlyCollection<UserCreationError> Errors);

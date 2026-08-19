using SmartExpense.Application.Abstractions.Authentication;

namespace SmartExpense.Application.Authentication;

public sealed class LoginUser(
    IIdentityService identityService,
    IAccessTokenService accessTokenService)
{
    public const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<LoginUserResult> ExecuteAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email) ||
            string.IsNullOrWhiteSpace(command.Password))
        {
            return LoginUserResult.Failure(InvalidCredentialsMessage);
        }

        var user = await identityService.FindByEmailAsync(
            command.Email,
            cancellationToken);

        if (user is null)
        {
            return LoginUserResult.Failure(InvalidCredentialsMessage);
        }

        var passwordIsValid = await identityService.CheckPasswordAsync(
            user.Id,
            command.Password,
            cancellationToken);

        if (!passwordIsValid)
        {
            return LoginUserResult.Failure(InvalidCredentialsMessage);
        }

        return LoginUserResult.Success(accessTokenService.Create(user));
    }
}

public sealed record LoginUserCommand(string Email, string Password);

public sealed record LoginUserResult(
    bool Succeeded,
    AccessToken? AccessToken,
    string? Error)
{
    public static LoginUserResult Success(AccessToken accessToken) =>
        new(true, accessToken, null);

    public static LoginUserResult Failure(string error) =>
        new(false, null, error);
}

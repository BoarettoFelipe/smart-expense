namespace SmartExpense.Api.Contracts.Authentication;

public sealed record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse(Guid UserId);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record AuthenticationErrorResponse(
    string Message,
    IReadOnlyCollection<AuthenticationValidationError>? Errors = null);

public sealed record AuthenticationValidationError(string Code, string Description);

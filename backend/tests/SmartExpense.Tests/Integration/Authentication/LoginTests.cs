using System.IdentityModel.Tokens.Jwt;
using SmartExpense.Application.Authentication;
using SmartExpense.Tests.Integration.Persistence;

namespace SmartExpense.Tests.Integration.Authentication;

[Collection(PostgreSqlCollection.Name)]
public sealed class LoginTests(PostgreSqlFixture fixture)
{
    private const string ValidPassword = "ValidPassword1!";

    [Fact]
    public async Task Login_WithValidCredentials_Succeeds()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = await RegisterUserAsync(testScope);

        var result = await testScope.LoginUser.ExecuteAsync(
            new LoginUserCommand(email, ValidPassword));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.AccessToken);
        Assert.NotEmpty(result.AccessToken.Value);
        Assert.True(result.AccessToken.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_ReturnsSameFailure()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = await RegisterUserAsync(testScope);

        var wrongPasswordResult = await testScope.LoginUser.ExecuteAsync(
            new LoginUserCommand(email, "WrongPassword1!"));
        var unknownEmailResult = await testScope.LoginUser.ExecuteAsync(
            new LoginUserCommand(
                $"unknown-{Guid.NewGuid():N}@example.com",
                ValidPassword));

        Assert.False(wrongPasswordResult.Succeeded);
        Assert.False(unknownEmailResult.Succeeded);
        Assert.Equal(LoginUser.InvalidCredentialsMessage, wrongPasswordResult.Error);
        Assert.Equal(wrongPasswordResult.Error, unknownEmailResult.Error);
    }

    [Fact]
    public async Task Login_SuccessfulTokenContainsCorrectSubjectAndEmail()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = await RegisterUserAsync(testScope);
        var user = await testScope.UserManager.FindByEmailAsync(email);

        var result = await testScope.LoginUser.ExecuteAsync(
            new LoginUserCommand(email, ValidPassword));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            result.AccessToken!.Value);

        Assert.NotNull(user);
        Assert.Equal(
            user.Id.ToString(),
            token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(
            email,
            token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public async Task Login_TokenDoesNotContainSensitiveCredentialData()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = await RegisterUserAsync(testScope);
        var user = await testScope.UserManager.FindByEmailAsync(email);

        var result = await testScope.LoginUser.ExecuteAsync(
            new LoginUserCommand(email, ValidPassword));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            result.AccessToken!.Value);

        Assert.NotNull(user);
        Assert.DoesNotContain(token.Claims, claim =>
            claim.Type.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            claim.Type.Contains("stamp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(token.Claims, claim =>
            claim.Value == ValidPassword || claim.Value == user.PasswordHash);
    }

    [Fact]
    public async Task Login_TokenPassesConfiguredSignatureIssuerAudienceAndLifetimeValidation()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = await RegisterUserAsync(testScope);

        var result = await testScope.LoginUser.ExecuteAsync(
            new LoginUserCommand(email, ValidPassword));
        var handler = new JwtSecurityTokenHandler();

        var principal = handler.ValidateToken(
            result.AccessToken!.Value,
            testScope.JwtOptions.CreateTokenValidationParameters(),
            out var validatedToken);

        Assert.NotNull(principal);
        Assert.IsType<JwtSecurityToken>(validatedToken);
    }

    private static async Task<string> RegisterUserAsync(
        AuthenticationTestScope testScope)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var result = await testScope.RegisterUser.ExecuteAsync(
            new RegisterUserCommand(email, ValidPassword));

        Assert.True(result.Succeeded);

        return email;
    }
}

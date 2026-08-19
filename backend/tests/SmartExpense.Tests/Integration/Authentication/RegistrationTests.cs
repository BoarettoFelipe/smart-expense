using SmartExpense.Application.Authentication;
using SmartExpense.Tests.Integration.Persistence;

namespace SmartExpense.Tests.Integration.Authentication;

[Collection(PostgreSqlCollection.Name)]
public sealed class RegistrationTests(PostgreSqlFixture fixture)
{
    private const string ValidPassword = "ValidPassword1!";

    [Fact]
    public async Task Register_WithValidCredentials_PersistsUserWithGuidAndEmail()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = CreateEmail();

        var result = await testScope.RegisterUser.ExecuteAsync(
            new RegisterUserCommand(email, ValidPassword));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.UserId);
        Assert.NotEqual(Guid.Empty, result.UserId.Value);

        var persistedUser = await testScope.UserManager.FindByEmailAsync(email);

        Assert.NotNull(persistedUser);
        Assert.Equal(result.UserId, persistedUser.Id);
        Assert.Equal(email, persistedUser.Email);
        Assert.Equal(email, persistedUser.UserName);
    }

    [Fact]
    public async Task Register_DoesNotStorePasswordAsPlaintext()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = CreateEmail();

        var result = await testScope.RegisterUser.ExecuteAsync(
            new RegisterUserCommand(email, ValidPassword));
        var persistedUser = await testScope.UserManager.FindByEmailAsync(email);

        Assert.True(result.Succeeded);
        Assert.NotNull(persistedUser);
        Assert.NotNull(persistedUser.PasswordHash);
        Assert.NotEqual(ValidPassword, persistedUser.PasswordHash);
        Assert.True(await testScope.UserManager.CheckPasswordAsync(
            persistedUser,
            ValidPassword));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Fails()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = CreateEmail();

        var firstResult = await testScope.RegisterUser.ExecuteAsync(
            new RegisterUserCommand(email, ValidPassword));
        var duplicateResult = await testScope.RegisterUser.ExecuteAsync(
            new RegisterUserCommand(email, ValidPassword));

        Assert.True(firstResult.Succeeded);
        Assert.False(duplicateResult.Succeeded);
        Assert.Contains(
            duplicateResult.Errors,
            error => error.Code is "DuplicateEmail" or "DuplicateUserName");
    }

    [Fact]
    public async Task Register_WithInvalidPassword_FailsIdentityPolicy()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);
        var email = CreateEmail();

        var result = await testScope.RegisterUser.ExecuteAsync(
            new RegisterUserCommand(email, "weak"));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error => error.Code.StartsWith("Password", StringComparison.Ordinal));
        Assert.Null(await testScope.UserManager.FindByEmailAsync(email));
    }

    [Fact]
    public async Task Register_WithBlankInput_ReturnsApplicationValidationErrors()
    {
        await using var testScope = AuthenticationTestScope.Create(fixture);

        var result = await testScope.RegisterUser.ExecuteAsync(
            new RegisterUserCommand(" ", string.Empty));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "EmailRequired");
        Assert.Contains(result.Errors, error => error.Code == "PasswordRequired");
    }

    private static string CreateEmail() =>
        $"user-{Guid.NewGuid():N}@example.com";
}

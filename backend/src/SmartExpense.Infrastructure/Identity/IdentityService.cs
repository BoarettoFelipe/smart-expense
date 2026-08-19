using Microsoft.AspNetCore.Identity;
using SmartExpense.Application.Abstractions.Authentication;

namespace SmartExpense.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager)
    : IIdentityService
{
    public async Task<UserCreationResult> CreateUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user, password);

        cancellationToken.ThrowIfCancellationRequested();

        var errors = result.Errors
            .Select(error => new UserCreationError(error.Code, error.Description))
            .ToArray();

        return new UserCreationResult(
            result.Succeeded,
            result.Succeeded ? user.Id : null,
            errors);
    }

    public async Task<UserAccount?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);

        cancellationToken.ThrowIfCancellationRequested();

        return user?.Email is null
            ? null
            : new UserAccount(user.Id, user.Email);
    }

    public async Task<bool> CheckPasswordAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return false;
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(user, password);

        cancellationToken.ThrowIfCancellationRequested();

        return passwordIsValid;
    }
}

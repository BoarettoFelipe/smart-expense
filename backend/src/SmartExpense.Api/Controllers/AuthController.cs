using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartExpense.Api.Contracts.Authentication;
using SmartExpense.Application.Authentication;

namespace SmartExpense.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(
    RegisterUser registerUser,
    LoginUser loginUser) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerUser.ExecuteAsync(
            new RegisterUserCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(error => new AuthenticationValidationError(
                    error.Code,
                    error.Description))
                .ToArray();

            return BadRequest(new AuthenticationErrorResponse(
                "Registration failed.",
                errors));
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new RegisterResponse(result.UserId!.Value));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUser.ExecuteAsync(
            new LoginUserCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(new AuthenticationErrorResponse(
                LoginUser.InvalidCredentialsMessage));
        }

        return Ok(new LoginResponse(
            result.AccessToken!.Value,
            result.AccessToken.ExpiresAt));
    }
}

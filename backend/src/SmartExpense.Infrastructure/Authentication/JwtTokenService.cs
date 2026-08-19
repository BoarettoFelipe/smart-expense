using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SmartExpense.Application.Abstractions.Authentication;

namespace SmartExpense.Infrastructure.Authentication;

public sealed class JwtTokenService(JwtOptions options) : IAccessTokenService
{
    public AccessToken Create(UserAccount user)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddMinutes(options.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                options.CreateSigningKey(),
                SecurityAlgorithms.HmacSha256));

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}

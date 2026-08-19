using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SmartExpense.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    private const int MinimumSigningKeyBytes = 32;
    private const int MaximumExpirationMinutes = 1440;
    private readonly string _signingKey;

    private JwtOptions(
        string issuer,
        string audience,
        string signingKey,
        int expirationMinutes)
    {
        Issuer = issuer;
        Audience = audience;
        _signingKey = signingKey;
        ExpirationMinutes = expirationMinutes;
    }

    public string Issuer { get; }

    public string Audience { get; }

    public int ExpirationMinutes { get; }

    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        var issuer = GetRequiredValue(configuration, "Issuer");
        var audience = GetRequiredValue(configuration, "Audience");
        var signingKey = GetRequiredValue(configuration, "SigningKey");
        var expirationValue = GetRequiredValue(configuration, "ExpirationMinutes");

        if (Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"Configuration '{SectionName}:SigningKey' must contain at least " +
                $"{MinimumSigningKeyBytes} UTF-8 bytes.");
        }

        if (!int.TryParse(expirationValue, out var expirationMinutes) ||
            expirationMinutes is <= 0 or > MaximumExpirationMinutes)
        {
            throw new InvalidOperationException(
                $"Configuration '{SectionName}:ExpirationMinutes' must be between 1 " +
                $"and {MaximumExpirationMinutes}.");
        }

        return new JwtOptions(issuer, audience, signingKey, expirationMinutes);
    }

    public TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    internal SymmetricSecurityKey CreateSigningKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
    }

    private static string GetRequiredValue(
        IConfiguration configuration,
        string name)
    {
        var key = $"{SectionName}:{name}";
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required configuration '{key}' was not found or is empty.");
        }

        return value;
    }
}

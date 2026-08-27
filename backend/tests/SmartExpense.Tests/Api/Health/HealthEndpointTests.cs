using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartExpense.Tests.Api.Health;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_WithoutBearerToken_ReturnsOk()
    {
        var signingKey = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused");
                builder.UseSetting("Jwt:Issuer", "SmartExpense.Tests");
                builder.UseSetting(
                    "Jwt:Audience",
                    "SmartExpense.Tests.Client");
                builder.UseSetting("Jwt:SigningKey", signingKey);
                builder.UseSetting("Jwt:ExpirationMinutes", "15");
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

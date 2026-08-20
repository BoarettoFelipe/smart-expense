using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartExpense.Tests.Api.Transactions;

public sealed class TransactionsAuthorizationTests
{
    [Fact]
    public async Task GetTransactions_WithoutBearerToken_ReturnsUnauthorized()
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

        var response = await client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

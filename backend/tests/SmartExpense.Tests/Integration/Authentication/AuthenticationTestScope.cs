using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Authentication;
using SmartExpense.Infrastructure;
using SmartExpense.Infrastructure.Authentication;
using SmartExpense.Infrastructure.Identity;
using SmartExpense.Tests.Integration.Persistence;

namespace SmartExpense.Tests.Integration.Authentication;

internal sealed class AuthenticationTestScope : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    private AuthenticationTestScope(
        ServiceProvider serviceProvider,
        IServiceScope scope,
        JwtOptions jwtOptions)
    {
        _serviceProvider = serviceProvider;
        _scope = scope;
        JwtOptions = jwtOptions;

        var identityService = Services.GetRequiredService<IIdentityService>();
        RegisterUser = new RegisterUser(identityService);
        LoginUser = new LoginUser(
            identityService,
            Services.GetRequiredService<IAccessTokenService>());
    }

    public IServiceProvider Services => _scope.ServiceProvider;

    public RegisterUser RegisterUser { get; }

    public LoginUser LoginUser { get; }

    public JwtOptions JwtOptions { get; }

    public UserManager<ApplicationUser> UserManager =>
        Services.GetRequiredService<UserManager<ApplicationUser>>();

    public static AuthenticationTestScope Create(PostgreSqlFixture fixture)
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString,
            ["Jwt:Issuer"] = "SmartExpense.Tests",
            ["Jwt:Audience"] = "SmartExpense.Tests.Client",
            ["Jwt:SigningKey"] = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64)),
            ["Jwt:ExpirationMinutes"] = "15"
        };

        var jwtOptions = JwtOptions.FromConfiguration(configuration);
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddInfrastructure(configuration, jwtOptions);

        var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        return new AuthenticationTestScope(
            serviceProvider,
            serviceProvider.CreateScope(),
            jwtOptions);
    }

    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _serviceProvider.DisposeAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartExpense.Application.Abstractions.Authentication;
using SmartExpense.Application.Abstractions.Dashboard;
using SmartExpense.Application.Abstractions.Persistence;
using SmartExpense.Infrastructure.Authentication;
using SmartExpense.Infrastructure.Dashboard;
using SmartExpense.Infrastructure.Identity;
using SmartExpense.Infrastructure.Persistence;
using SmartExpense.Infrastructure.Persistence.Repositories;

namespace SmartExpense.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddInfrastructure(
            configuration,
            JwtOptions.FromConfiguration(configuration));
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        JwtOptions jwtOptions)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found or is empty.");
        }

        services.AddDbContext<SmartExpenseDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
            options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<SmartExpenseDbContext>();

        services.AddSingleton(jwtOptions);
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IAccessTokenService, JwtTokenService>();

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IDashboardReadService, DashboardReadService>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<SmartExpenseDbContext>());

        return services;
    }
}

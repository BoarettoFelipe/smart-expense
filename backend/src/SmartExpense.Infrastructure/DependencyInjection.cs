using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartExpense.Application.Abstractions.Persistence;
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
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found or is empty.");
        }

        services.AddDbContext<SmartExpenseDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<SmartExpenseDbContext>();

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<SmartExpenseDbContext>());

        return services;
    }
}

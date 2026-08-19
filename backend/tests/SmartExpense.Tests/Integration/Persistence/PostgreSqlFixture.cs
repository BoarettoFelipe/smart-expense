using Microsoft.EntityFrameworkCore;
using SmartExpense.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartExpense.Tests.Integration.Persistence;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlFixture()
    {
        _container = new PostgreSqlBuilder("postgres:18.4-alpine")
            .WithDatabase("smart_expense_tests")
            .WithUsername("smart_expense_tests")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
    }

    public bool MigrationsApplied { get; private set; }

    public SmartExpenseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SmartExpenseDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new SmartExpenseDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        MigrationsApplied = appliedMigrations.Any(
            migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));

        if (!MigrationsApplied)
        {
            throw new InvalidOperationException("InitialCreate was not applied to the test database.");
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.API.Data;

public static class DatabaseSeeder
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (environment is null) throw new ArgumentNullException(nameof(environment));
        if (logger is null) throw new ArgumentNullException(nameof(logger));

        var seedingEnabled = configuration.GetValue<bool?>("DatabaseSeeding:Enabled") ?? environment.IsDevelopment();
        var resetDatabaseOnStartup = seedingEnabled && (configuration.GetValue<bool?>("DatabaseSeeding:ResetDatabaseOnStartup") ?? false);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var providerName = db.Database.ProviderName ?? string.Empty;

        if (resetDatabaseOnStartup)
        {
            logger.LogWarning("Database reset requested. Deleting and recreating the database.");

            if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.EnsureDeletedAsync(cancellationToken);
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
            else if (db.Database.IsRelational())
            {
                await db.Database.EnsureDeletedAsync(cancellationToken);
                await db.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                await db.Database.EnsureDeletedAsync(cancellationToken);
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
        }
        else if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
        else if (db.Database.IsRelational())
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Relational-specific methods", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!seedingEnabled)
        {
            logger.LogInformation("Database schema is ready. Sample data seeding is disabled.");
            return;
        }

        // Public repository intentionally does not include seeded users, passwords, or demo accounts.
        // Add local/private seed data here only when needed, and keep real credentials in private configuration.
        logger.LogInformation("Database schema is ready. No public sample data was seeded.");
    }
}

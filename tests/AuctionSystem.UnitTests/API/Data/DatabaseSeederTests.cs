using AuctionSystem.API.Data;
using AuctionSystem.Domain.Users;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuctionSystem.UnitTests.API.Data;

public class DatabaseSeederTests
{
    [Fact]
    public async Task InitializeAsync_WhenSeedingDisabled_DoesNotSeedSampleData()
    {
        using var provider = CreateServiceProvider(Guid.NewGuid().ToString("N"));
        var configuration = CreateConfiguration(enabled: false, resetDatabaseOnStartup: false);
        var environment = new TestHostEnvironment(Environments.Development);

        await DatabaseSeeder.InitializeAsync(provider, configuration, environment, NullLogger.Instance);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(0, await db.Users.CountAsync());
        Assert.Equal(0, await db.Auctions.CountAsync());
        Assert.Equal(0, await db.UserPasswords.CountAsync());
    }

    [Fact]
    public async Task InitializeAsync_WhenSeedingEnabled_SeedsExpectedBaselineData()
    {
        using var provider = CreateServiceProvider(Guid.NewGuid().ToString("N"));
        var configuration = CreateConfiguration(enabled: true, resetDatabaseOnStartup: false);
        var environment = new TestHostEnvironment(Environments.Production);

        await DatabaseSeeder.InitializeAsync(provider, configuration, environment, NullLogger.Instance);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(0, await db.Users.CountAsync());
        Assert.Equal(0, await db.Auctions.CountAsync());
        Assert.Equal(0, await db.Bids.CountAsync());
        Assert.Equal(0, await db.UserPasswords.CountAsync());
        Assert.Equal(0, await db.AdminSystemSettings.CountAsync());
    }

    [Fact]
    public async Task InitializeAsync_WhenUsersAlreadyExistWithoutReset_SkipsSeeding()
    {
        using var provider = CreateServiceProvider(Guid.NewGuid().ToString("N"));
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(User.Register("existing@example.com", "Existing User", UserRole.Bidder));
            await db.SaveChangesAsync();
        }

        var configuration = CreateConfiguration(enabled: true, resetDatabaseOnStartup: false);
        var environment = new TestHostEnvironment(Environments.Development);

        await DatabaseSeeder.InitializeAsync(provider, configuration, environment, NullLogger.Instance);

        using var verificationScope = provider.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(1, await verificationDb.Users.CountAsync());
        Assert.True(await verificationDb.Users.AnyAsync(x => x.Email == "existing@example.com"));
    }

    [Fact]
    public async Task InitializeAsync_WhenResetRequested_RecreatesAndReseedsDatabase()
    {
        using var provider = CreateServiceProvider(Guid.NewGuid().ToString("N"));
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(User.Register("stale@example.com", "Stale User", UserRole.Bidder));
            await db.SaveChangesAsync();
        }

        var configuration = CreateConfiguration(enabled: true, resetDatabaseOnStartup: true);
        var environment = new TestHostEnvironment(Environments.Development);

        await DatabaseSeeder.InitializeAsync(provider, configuration, environment, NullLogger.Instance);

        using var verificationScope = provider.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(0, await verificationDb.Users.CountAsync());
        Assert.False(await verificationDb.Users.AnyAsync(x => x.Email == "stale@example.com"));
        Assert.Equal(0, await verificationDb.Auctions.CountAsync());
    }

    private static ServiceProvider CreateServiceProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateConfiguration(bool enabled, bool resetDatabaseOnStartup)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSeeding:Enabled"] = enabled.ToString(),
                ["DatabaseSeeding:ResetDatabaseOnStartup"] = resetDatabaseOnStartup.ToString()
            })
            .Build();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "AuctionSystem.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}

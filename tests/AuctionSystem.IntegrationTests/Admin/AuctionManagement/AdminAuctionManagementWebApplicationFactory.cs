using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AuctionSystem.IntegrationTests.Admin.AuctionManagement;

public sealed class AdminAuctionManagementWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AuctionSystem-AdminAuctionManagementTests-{Guid.NewGuid()}";

    public Mock<IPasswordVerifier> PasswordVerifierMock { get; } = new();
    public Mock<IPasswordStore> PasswordStoreMock { get; } = new();
    public Mock<ITokenService> TokenServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            var sqlServerDescriptors = services
                .Where(d => (d.ServiceType.FullName?.Contains("SqlServer") ?? false) ||
                            (d.ImplementationType?.FullName?.Contains("SqlServer") ?? false))
                .ToList();

            foreach (var descriptor in sqlServerDescriptors)
            {
                services.Remove(descriptor);
            }

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
                    .UseInternalServiceProvider(inMemoryProvider));

            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IAuctionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuctionRepository, AuctionRepository>();

            services.RemoveAll<IPasswordVerifier>();
            services.RemoveAll<IPasswordStore>();
            services.RemoveAll<ITokenService>();

            services.AddSingleton(PasswordVerifierMock.Object);
            services.AddSingleton(PasswordStoreMock.Object);
            services.AddSingleton(TokenServiceMock.Object);
        });
    }

    public async Task<Guid> SeedUserAsync(string email, string fullName, UserRole role, bool isActive = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = User.Register(email, fullName, role);
        if (!isActive)
        {
            user.Deactivate();
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    public async Task<Guid> SeedAuctionAsync(
        Guid sellerId,
        string title = "Seeded Auction",
        decimal startingPriceAmount = 100m,
        string currency = "USD",
        bool startAuction = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var auction = Auction.Create(
            sellerId,
            title,
            Money.Create(startingPriceAmount, currency),
            DateTime.UtcNow.AddHours(6),
            "Seeded auction description",
            "Collectibles");

        if (startAuction)
        {
            auction.Start();
        }

        db.Auctions.Add(auction);
        await db.SaveChangesAsync();

        return auction.Id;
    }
}

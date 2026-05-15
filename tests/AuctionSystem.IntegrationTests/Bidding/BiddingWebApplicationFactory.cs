using AuctionSystem.Application.Abstractions.Realtime;
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

namespace AuctionSystem.IntegrationTests.Bidding;

public sealed class BiddingWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AuctionSystem-BiddingTests-{Guid.NewGuid()}";
    private readonly bool _useMockRealtimeNotifier;

    public BiddingWebApplicationFactory(bool useMockRealtimeNotifier = true)
    {
        _useMockRealtimeNotifier = useMockRealtimeNotifier;
    }

    public Mock<IPasswordVerifier> PasswordVerifierMock { get; } = new();
    public Mock<IPasswordStore> PasswordStoreMock { get; } = new();
    public Mock<ITokenService> TokenServiceMock { get; } = new();
    public Mock<IAuctionRealtimeNotifier> RealtimeNotifierMock { get; } = new();

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

            if (_useMockRealtimeNotifier)
            {
                services.RemoveAll<IAuctionRealtimeNotifier>();
                services.AddSingleton(RealtimeNotifierMock.Object);
            }
        });
    }

    public async Task<(Guid SellerId, Guid BidderId, Guid AuctionId)> SeedActiveAuctionAsync(
        decimal startingPriceAmount = 100m,
        string currency = "USD")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);

        var auction = Auction.Create(
            seller.Id,
            "Gaming Laptop",
            Money.Create(startingPriceAmount, currency),
            DateTime.UtcNow.AddHours(2),
            "High-end laptop");

        auction.Start();

        db.Users.Add(seller);
        db.Users.Add(bidder);
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();

        return (seller.Id, bidder.Id, auction.Id);
    }
}
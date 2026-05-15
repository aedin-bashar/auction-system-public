using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.IntegrationTests.Infrastructure;

public class PersistenceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AuctionSystem-TestDb-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task UserRepository_AddAndGetByEmail_Works()
    {
        await using var db = CreateDbContext();
        var repo = new UserRepository(db);

        var user = User.Register("John.Doe@Example.com", "John Doe", role: UserRole.Bidder);
        await repo.AddAsync(user);
        await db.SaveChangesAsync();

        var loaded = await repo.GetByEmailAsync("  john.doe@example.com  ");

        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded!.Id);
        Assert.Equal("john.doe@example.com", loaded.Email);
        Assert.Equal(UserRole.Bidder, loaded.Role);
    }

    [Fact]
    public async Task AuctionRepository_GetWithBidsByIdAsync_IncludesBids()
    {
        await using var db = CreateDbContext();
        var repo = new AuctionRepository(db);

        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        var sellerId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();

        var auction = Auction.Create(
            sellerId,
            "Test Auction",
            Money.Create(10m, "USD"),
            endTimeUtc: now.AddDays(1),
            nowUtc: now);

        auction.Start(now.AddMinutes(1));
        auction.PlaceBid(bidderId, Money.Create(12m, "USD"), nowUtc: now.AddMinutes(2));

        await repo.AddAsync(auction);
        await db.SaveChangesAsync();

        var loaded = await repo.GetWithBidsByIdAsync(auction.Id);

        Assert.NotNull(loaded);
        Assert.Equal(auction.Id, loaded!.Id);
        Assert.Single(loaded.Bids);
        Assert.Equal(12m, loaded.CurrentPrice.Amount);
        Assert.Equal("USD", loaded.CurrentPrice.Currency);
    }
}
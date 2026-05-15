using AuctionSystem.API.Data;
using AuctionSystem.API.DTOs;
using AuctionSystem.API.Models;
using AuctionSystem.API.Services;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Tests;

public class AuctionServiceTests
{
    private AuctionDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AuctionDbContext(options);
    }

    [Fact]
    public async Task GetAllAuctions_ReturnsAllAuctions()
    {
        using var context = CreateContext(nameof(GetAllAuctions_ReturnsAllAuctions));
        context.Auctions.AddRange(
            new Auction { Title = "Auction 1", Description = "Desc 1", StartingPrice = 100, CurrentPrice = 100, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddDays(1) },
            new Auction { Title = "Auction 2", Description = "Desc 2", StartingPrice = 200, CurrentPrice = 200, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddDays(2) }
        );
        await context.SaveChangesAsync();

        var service = new AuctionService(context);
        var result = await service.GetAllAuctions();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateAuction_ShouldAddAuction()
    {
        using var context = CreateContext(nameof(CreateAuction_ShouldAddAuction));
        var service = new AuctionService(context);
        var dto = new CreateAuctionDto
        {
            Title = "Test Auction",
            Description = "Test Description",
            StartingPrice = 50,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddDays(1)
        };

        var result = await service.CreateAuction(dto);

        Assert.NotNull(result);
        Assert.Equal("Test Auction", result.Title);
        Assert.Equal(50, result.CurrentPrice);
        Assert.Equal(1, await context.Auctions.CountAsync());
    }

    [Fact]
    public async Task PlaceBid_ValidBid_ShouldUpdateCurrentPrice()
    {
        using var context = CreateContext(nameof(PlaceBid_ValidBid_ShouldUpdateCurrentPrice));
        var auction = new Auction
        {
            Title = "Test",
            Description = "Test",
            StartingPrice = 100,
            CurrentPrice = 100,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddDays(1)
        };
        context.Auctions.Add(auction);
        context.Users.Add(new User { Id = 1, Username = "bidder", PasswordHash = "hash", Role = "Bidder" });
        await context.SaveChangesAsync();

        var service = new AuctionService(context);
        var bid = await service.PlaceBid(auction.Id, 1, 150);

        Assert.Equal(150, bid.Amount);
        var updated = await context.Auctions.FindAsync(auction.Id);
        Assert.Equal(150, updated!.CurrentPrice);
    }

    [Fact]
    public async Task PlaceBid_BidTooLow_ShouldThrow()
    {
        using var context = CreateContext(nameof(PlaceBid_BidTooLow_ShouldThrow));
        var auction = new Auction
        {
            Title = "Test",
            Description = "Test",
            StartingPrice = 100,
            CurrentPrice = 100,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddDays(1)
        };
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        var service = new AuctionService(context);
        await Assert.ThrowsAsync<Exception>(() => service.PlaceBid(auction.Id, 1, 50));
    }

    [Fact]
    public async Task PlaceBid_AuctionExpired_ShouldThrow()
    {
        using var context = CreateContext(nameof(PlaceBid_AuctionExpired_ShouldThrow));
        var auction = new Auction
        {
            Title = "Test",
            Description = "Test",
            StartingPrice = 100,
            CurrentPrice = 100,
            StartTime = DateTime.UtcNow.AddDays(-2),
            EndTime = DateTime.UtcNow.AddDays(-1)
        };
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        var service = new AuctionService(context);
        await Assert.ThrowsAsync<Exception>(() => service.PlaceBid(auction.Id, 1, 150));
    }
}

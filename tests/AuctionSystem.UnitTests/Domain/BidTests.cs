using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.ValueObjects;

namespace AuctionSystem.UnitTests.Domain;

public class BidTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var auctionId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var placedAt = new DateTime(2030, 01, 01, 10, 0, 0, DateTimeKind.Utc);

        var bid = Bid.Create(auctionId, bidderId, Money.Create(12.34m, "USD"), placedAt);

        Assert.NotEqual(Guid.Empty, bid.Id);
        Assert.Equal(auctionId, bid.AuctionId);
        Assert.Equal(bidderId, bid.BidderId);
        Assert.Equal(12.34m, bid.Amount.Amount);
        Assert.Equal("USD", bid.Amount.Currency);
        Assert.Equal(placedAt, bid.PlacedAtUtc);
        Assert.Equal(DateTimeKind.Utc, bid.PlacedAtUtc.Kind);
    }

    [Fact]
    public void Create_WithEmptyAuctionId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Bid.Create(Guid.Empty, Guid.NewGuid(), Money.Create(1m, "USD"), DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyBidderId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Bid.Create(Guid.NewGuid(), Guid.Empty, Money.Create(1m, "USD"), DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Bid.Create(Guid.NewGuid(), Guid.NewGuid(), Money.Create(0m, "USD"), DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithUnspecifiedDateTime_CoercesToUtcKind()
    {
        var unspecified = new DateTime(2030, 01, 01, 10, 0, 0, DateTimeKind.Unspecified);

        var bid = Bid.Create(Guid.NewGuid(), Guid.NewGuid(), Money.Create(1m, "USD"), unspecified);

        Assert.Equal(DateTimeKind.Utc, bid.PlacedAtUtc.Kind);
        Assert.Equal(unspecified, bid.PlacedAtUtc);
    }
}
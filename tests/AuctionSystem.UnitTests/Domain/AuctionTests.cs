using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.ValueObjects;

namespace AuctionSystem.UnitTests.Domain;

public class AuctionTests
{
    [Fact]
    public void Create_WithPastEndTime_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2029, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Auction.Create(Guid.NewGuid(), "Test Auction", Money.Create(10m, "USD"), end, nowUtc: now));
    }

    [Fact]
    public void Create_WithEndTimeEqualToNow_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Auction.Create(Guid.NewGuid(), "Test Auction", Money.Create(10m, "USD"), endTimeUtc: now, nowUtc: now));
    }

    [Fact]
    public void Create_WithUnspecifiedEndTime_CoercesToUtcKind()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var endUnspecified = new DateTime(2030, 01, 02, 0, 0, 0, DateTimeKind.Unspecified);

        var auction = Auction.Create(
            Guid.NewGuid(),
            "Test Auction",
            Money.Create(10m, "USD"),
            endTimeUtc: endUnspecified,
            nowUtc: now);

        Assert.Equal(DateTimeKind.Utc, auction.EndTimeUtc.Kind);
        Assert.Equal(endUnspecified, auction.EndTimeUtc);
    }

    [Fact]
    public void Start_FromDraft_SetsActiveAndStartTime()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var auction = Auction.Create(Guid.NewGuid(), "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);

        auction.Start(now.AddMinutes(1));

        Assert.Equal(AuctionStatus.Active, auction.Status);
        Assert.Equal(now.AddMinutes(1), auction.StartTimeUtc);
    }

    [Fact]
    public void Start_WhenNotDraft_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var auction = Auction.Create(Guid.NewGuid(), "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);

        auction.Start(now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => auction.Start(now.AddMinutes(2)));
    }

    [Fact]
    public void PlaceBid_WhenNotActive_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var auction = Auction.Create(Guid.NewGuid(), "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);

        Assert.Throws<InvalidOperationException>(() =>
            auction.PlaceBid(Guid.NewGuid(), Money.Create(11m, "USD"), nowUtc: now.AddMinutes(1)));
    }

    [Fact]
    public void PlaceBid_WithCurrencyMismatch_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var auction = Auction.Create(Guid.NewGuid(), "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);
        auction.Start(now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            auction.PlaceBid(Guid.NewGuid(), Money.Create(11m, "EUR"), nowUtc: now.AddMinutes(2)));
    }

    [Fact]
    public void PlaceBid_LessThanMinimumIncrement_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var auction = Auction.Create(Guid.NewGuid(), "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);
        auction.Start(now.AddMinutes(1));

        // equal to starting price
        Assert.Throws<InvalidOperationException>(() =>
            auction.PlaceBid(Guid.NewGuid(), Money.Create(10m, "USD"), nowUtc: now.AddMinutes(2)));

        // less than required +1 increment from starting price
        Assert.Throws<InvalidOperationException>(() =>
            auction.PlaceBid(Guid.NewGuid(), Money.Create(10.5m, "USD"), nowUtc: now.AddMinutes(3)));

        // first valid bid (current 10 -> min 11)
        auction.PlaceBid(Guid.NewGuid(), Money.Create(11m, "USD"), nowUtc: now.AddMinutes(3));

        // equal to current price
        Assert.Throws<InvalidOperationException>(() =>
            auction.PlaceBid(Guid.NewGuid(), Money.Create(11m, "USD"), nowUtc: now.AddMinutes(4)));

        // less than required +1 increment from current price (current 11 -> min 12)
        Assert.Throws<InvalidOperationException>(() =>
            auction.PlaceBid(Guid.NewGuid(), Money.Create(11.5m, "USD"), nowUtc: now.AddMinutes(5)));
    }

    [Fact]
    public void PlaceBid_UpdatesBidsAndCurrentPrice()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var sellerId = Guid.NewGuid();
        var bidder1 = Guid.NewGuid();
        var bidder2 = Guid.NewGuid();

        var auction = Auction.Create(sellerId, "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);
        auction.Start(now.AddMinutes(1));

        var bid1 = auction.PlaceBid(bidder1, Money.Create(12m, "USD"), nowUtc: now.AddMinutes(2));
        var bid2 = auction.PlaceBid(bidder2, Money.Create(15m, "USD"), nowUtc: now.AddMinutes(3));

        Assert.Equal(2, auction.Bids.Count);
        Assert.Equal(12m, bid1.Amount.Amount);
        Assert.Equal(15m, bid2.Amount.Amount);
        Assert.Equal(15m, auction.CurrentPrice.Amount);
    }

    [Fact]
    public void PlaceBid_BySeller_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var sellerId = Guid.NewGuid();

        var auction = Auction.Create(sellerId, "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);
        auction.Start(now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            auction.PlaceBid(sellerId, Money.Create(11m, "USD"), nowUtc: now.AddMinutes(2)));
    }

    [Fact]
    public void PlaceBid_AfterEndTime_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var end = now.AddMinutes(10);

        var auction = Auction.Create(Guid.NewGuid(), "Auction", Money.Create(10m, "USD"), end, nowUtc: now);
        auction.Start(now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            auction.PlaceBid(Guid.NewGuid(), Money.Create(11m, "USD"), nowUtc: end));
    }

    [Fact]
    public void End_WhenNotActive_Throws()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var auction = Auction.Create(Guid.NewGuid(), "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);

        Assert.Throws<InvalidOperationException>(() => auction.End(now.AddMinutes(1)));
    }

    [Fact]
    public void End_FromActive_SetsEnded()
    {
        var now = new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var auction = Auction.Create(Guid.NewGuid(), "Auction", Money.Create(10m, "USD"), now.AddDays(1), nowUtc: now);
        auction.Start(now.AddMinutes(1));

        var endNow = now.AddMinutes(5);
        auction.End(endNow);

        Assert.Equal(AuctionStatus.Ended, auction.Status);
        Assert.Equal(endNow, auction.EndedAtUtc);
        Assert.Equal(endNow, auction.UpdatedAtUtc);
        Assert.Throws<InvalidOperationException>(() => auction.End(endNow.AddMinutes(1)));
    }
}

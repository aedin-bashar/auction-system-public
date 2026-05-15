using System.Linq.Expressions;
using AuctionSystem.Application.Auctions.MyBids;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.ValueObjects;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.MyBids;

public class GetMyBidsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyActiveAuctionsWhereBidderHasBids()
    {
        var now = DateTime.UtcNow;
        var bidderId = Guid.NewGuid();
        var otherBidderId = Guid.NewGuid();

        var matching = CreateActiveAuction("Vintage Console", "Gaming", 500m, now.AddHours(2), now.AddMinutes(-10));
        matching.PlaceBid(otherBidderId, Money.Create(520m, "USD"), now.AddMinutes(-9));
        matching.PlaceBid(bidderId, Money.Create(540m, "USD"), now.AddMinutes(-8));
        matching.PlaceBid(bidderId, Money.Create(560m, "USD"), now.AddMinutes(-7));

        var noMyBid = CreateActiveAuction("Smart Watch", "Tech", 250m, now.AddHours(3), now.AddMinutes(-12));
        noMyBid.PlaceBid(otherBidderId, Money.Create(270m, "USD"), now.AddMinutes(-11));

        var seeded = new List<Auction> { matching, noMyBid };

        var auctions = new Mock<IAuctionRepository>();
        auctions
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<Auction, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Auction, bool>> predicate, CancellationToken _) =>
                seeded.Where(predicate.Compile()).ToList());

        var handler = new GetMyBidsQueryHandler(auctions.Object);

        var result = await handler.Handle(new GetMyBidsQuery(bidderId), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(matching.Id, item.AuctionId);
        Assert.Equal(560m, item.MyMaxBidAmount);
        Assert.Equal(560m, item.CurrentHighestBidAmount);
        Assert.Equal(3, item.BidCount);
    }

    [Fact]
    public async Task Handle_WithEmptyBidderId_ThrowsArgumentException()
    {
        var auctions = new Mock<IAuctionRepository>();
        var handler = new GetMyBidsQueryHandler(auctions.Object);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.Handle(new GetMyBidsQuery(Guid.Empty), CancellationToken.None));
    }

    private static Auction CreateActiveAuction(string title, string category, decimal startPrice, DateTime endTimeUtc, DateTime nowUtc)
    {
        var auction = Auction.Create(
            sellerId: Guid.NewGuid(),
            title: title,
            startingPrice: Money.Create(startPrice, "USD"),
            endTimeUtc: endTimeUtc,
            description: "test",
            category: category,
            nowUtc: nowUtc);

        auction.Start(nowUtc.AddMinutes(1));
        return auction;
    }
}

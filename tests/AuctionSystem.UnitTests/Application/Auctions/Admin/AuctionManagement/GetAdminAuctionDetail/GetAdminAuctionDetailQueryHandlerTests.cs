using AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctionDetail;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement;

public class GetAdminAuctionDetailQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsDetailedAuctionProjection()
    {
        var now = DateTime.UtcNow;
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin, nowUtc: now.AddHours(-10));
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller, nowUtc: now.AddHours(-9));
        var bidderOne = User.Register("bidder1@example.com", "Bidder One", UserRole.Bidder, nowUtc: now.AddHours(-8));
        var bidderTwo = User.Register("bidder2@example.com", "Bidder Two", UserRole.Bidder, nowUtc: now.AddHours(-7));

        var auction = Auction.Create(
            seller.Id,
            "Collector Watch",
            Money.Create(100m, "USD"),
            now.AddDays(7),
            "Auction description",
            "Watches",
            now.AddHours(-6));

        auction.AddImage("second.png", "image/png", new byte[] { 2 }, 2, now.AddHours(-5));
        var firstImage = auction.AddImage("first.png", "image/png", new byte[] { 1 }, 0, now.AddHours(-5));
        auction.Start(now.AddHours(-4));
        var firstBid = auction.PlaceBid(bidderOne.Id, Money.Create(150m, "USD"), now.AddHours(-3));
        var secondBid = auction.PlaceBid(bidderTwo.Id, Money.Create(175m, "USD"), now.AddHours(-2));

        var query = new GetAdminAuctionDetailQuery(admin.Id, auction.Id);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetWithBidsByIdAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        users.Setup(x => x.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { seller, bidderOne, bidderTwo });

        var handler = new GetAdminAuctionDetailQueryHandler(users.Object, auctions.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(auction.Id, result.AuctionId);
        Assert.Equal(seller.FullName, result.SellerName);
        Assert.Equal(175m, result.CurrentBidAmount);
        Assert.Equal(2, result.BidCount);
        Assert.Equal("Bidder Two", result.HighestBidderName);
        Assert.Equal(firstImage.Id, result.PrimaryImageId);
        Assert.Equal(2, result.ImageCount);
        Assert.Equal(2, result.Bids.Count);
        Assert.Equal(secondBid.Id, result.Bids[0].BidId);
        Assert.Equal(firstBid.Id, result.Bids[1].BidId);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        var query = new GetAdminAuctionDetailQuery(seller.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();

        users.Setup(x => x.GetByIdAsync(seller.Id, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        var handler = new GetAdminAuctionDetailQueryHandler(users.Object, auctions.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));

        auctions.Verify(x => x.GetWithBidsByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuctionNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var query = new GetAdminAuctionDetailQuery(admin.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetWithBidsByIdAsync(query.AuctionId, It.IsAny<CancellationToken>())).ReturnsAsync((Auction?)null);

        var handler = new GetAdminAuctionDetailQueryHandler(users.Object, auctions.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}
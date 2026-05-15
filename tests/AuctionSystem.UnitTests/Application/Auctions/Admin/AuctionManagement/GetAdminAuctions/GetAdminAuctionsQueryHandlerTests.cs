using AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctions;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement;

public class GetAdminAuctionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsOrderedAuctionList()
    {
        var now = DateTime.UtcNow;
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin, nowUtc: now.AddHours(-8));
        var sellerOne = User.Register("seller1@example.com", "Seller One", UserRole.Seller, nowUtc: now.AddHours(-7));
        var sellerTwo = User.Register("seller2@example.com", "Seller Two", UserRole.Seller, nowUtc: now.AddHours(-6));

        var activeAuction = CreateAuction(sellerOne.Id, "Active Auction", now.AddHours(-2));
        activeAuction.Start(now.AddHours(-1));
        var draftAuction = CreateAuction(sellerTwo.Id, "Draft Auction", now.AddHours(-3));
        var endedAuction = CreateAuction(sellerOne.Id, "Ended Auction", now.AddHours(-4));
        endedAuction.Start(now.AddHours(-3));
        endedAuction.End(now.AddHours(-1));

        var query = new GetAdminAuctionsQuery(admin.Id);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Auction, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { endedAuction, draftAuction, activeAuction });
        users.Setup(x => x.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { sellerOne, sellerTwo });

        var handler = new GetAdminAuctionsQueryHandler(users.Object, auctions.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(activeAuction.Id, result[0].AuctionId);
        Assert.Equal(draftAuction.Id, result[1].AuctionId);
        Assert.Equal(endedAuction.Id, result[2].AuctionId);
        Assert.Equal("Seller One", result[0].SellerName);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        var query = new GetAdminAuctionsQuery(seller.Id);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();

        users.Setup(x => x.GetByIdAsync(seller.Id, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        var handler = new GetAdminAuctionsQueryHandler(users.Object, auctions.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));

        auctions.Verify(x => x.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Auction, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Auction CreateAuction(Guid sellerId, string title, DateTime nowUtc)
    {
        return Auction.Create(
            sellerId,
            title,
            Money.Create(100m, "USD"),
            nowUtc.AddDays(7),
            "Auction description",
            "Watches",
            nowUtc);
    }
}
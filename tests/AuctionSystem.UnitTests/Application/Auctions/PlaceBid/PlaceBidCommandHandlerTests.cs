using AuctionSystem.Application.Abstractions.Realtime;
using AuctionSystem.Application.Auctions.PlaceBid;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.PlaceBid;

public class PlaceBidCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidBidderAndAuction_PlacesBidSavesAndNotifies()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var auction = CreateActiveAuction();
        var command = new PlaceBidCommand(auction.Id, bidder.Id, 150m, "USD");

        var auctions = new Mock<IAuctionRepository>();
        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var realtimeNotifier = new Mock<IAuctionRealtimeNotifier>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);
        auctions.Setup(x => x.GetWithBidsByIdAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new PlaceBidCommandHandler(
            auctions.Object,
            users.Object,
            unitOfWork.Object,
            realtimeNotifier.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(auction.Id, result.AuctionId);
        Assert.Equal(bidder.Id, result.BidderId);
        Assert.Equal(150m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(150m, result.CurrentPriceAmount);
        Assert.Single(auction.Bids);

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        realtimeNotifier.Verify(
            x => x.NotifyBidPlacedAsync(
                It.Is<BidPlacedRealtimeEvent>(e =>
                    e.AuctionId == auction.Id &&
                    e.BidderId == bidder.Id &&
                    e.Amount == 150m &&
                    e.Currency == "USD" &&
                    e.CurrentPriceAmount == 150m &&
                    e.CurrentPriceCurrency == "USD"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBidderNotFound_ThrowsKeyNotFoundException()
    {
        var auction = CreateActiveAuction();
        var command = new PlaceBidCommand(auction.Id, Guid.NewGuid(), 150m, "USD");

        var auctions = new Mock<IAuctionRepository>();
        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var realtimeNotifier = new Mock<IAuctionRealtimeNotifier>();

        users.Setup(x => x.GetByIdAsync(command.BidderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new PlaceBidCommandHandler(
            auctions.Object,
            users.Object,
            unitOfWork.Object,
            realtimeNotifier.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        realtimeNotifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenBidderIsInactive_ThrowsInvalidOperationException()
    {
        var bidder = User.Register("inactive@example.com", "Inactive Bidder", UserRole.Bidder);
        bidder.Deactivate();
        var command = new PlaceBidCommand(Guid.NewGuid(), bidder.Id, 150m, "USD");

        var auctions = new Mock<IAuctionRepository>();
        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var realtimeNotifier = new Mock<IAuctionRealtimeNotifier>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);

        var handler = new PlaceBidCommandHandler(
            auctions.Object,
            users.Object,
            unitOfWork.Object,
            realtimeNotifier.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        realtimeNotifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenUserRoleIsNotBidder_ThrowsInvalidOperationException()
    {
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        var command = new PlaceBidCommand(Guid.NewGuid(), seller.Id, 150m, "USD");

        var auctions = new Mock<IAuctionRepository>();
        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var realtimeNotifier = new Mock<IAuctionRealtimeNotifier>();

        users.Setup(x => x.GetByIdAsync(seller.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seller);

        var handler = new PlaceBidCommandHandler(
            auctions.Object,
            users.Object,
            unitOfWork.Object,
            realtimeNotifier.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        realtimeNotifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenAuctionNotFound_ThrowsKeyNotFoundException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var auctionId = Guid.NewGuid();
        var command = new PlaceBidCommand(auctionId, bidder.Id, 150m, "USD");

        var auctions = new Mock<IAuctionRepository>();
        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var realtimeNotifier = new Mock<IAuctionRealtimeNotifier>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);
        auctions.Setup(x => x.GetWithBidsByIdAsync(auctionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Auction?)null);

        var handler = new PlaceBidCommandHandler(
            auctions.Object,
            users.Object,
            unitOfWork.Object,
            realtimeNotifier.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        realtimeNotifier.VerifyNoOtherCalls();
    }

    private static Auction CreateActiveAuction()
    {
        var sellerId = Guid.NewGuid();
        var auction = Auction.Create(
            sellerId,
            "Collector Watch",
            Money.Create(100m, "USD"),
            DateTime.UtcNow.AddDays(7),
            "Auction description",
            "Watches",
            DateTime.UtcNow.AddMinutes(-5));

        auction.Start(DateTime.UtcNow.AddMinutes(-4));
        return auction;
    }
}
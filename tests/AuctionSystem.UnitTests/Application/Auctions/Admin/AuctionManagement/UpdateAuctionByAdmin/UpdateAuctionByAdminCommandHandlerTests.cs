using AuctionSystem.Application.Auctions.Admin.AuctionManagement.UpdateAuctionByAdmin;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using MediatR;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement;

public class UpdateAuctionByAdminCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidAdminUpdate_UpdatesAuctionAndReplacesImages()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var auction = CreateActiveAuction();
        auction.AddImage("old.png", "image/png", new byte[] { 1 }, 0);

        var command = new UpdateAuctionByAdminCommand(
            admin.Id,
            auction.Id,
            "  Updated Title  ",
            "  Jewellery  ",
            "  Updated description  ",
            250m,
            "EUR",
            DateTime.UtcNow.AddDays(10),
            true,
            new[]
            {
                new UpdateAuctionImageInput("second.png", "image/png", new byte[] { 2 }, 2),
                new UpdateAuctionImageInput("first.png", "image/png", new byte[] { 1 }, 0)
            });

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetWithBidsByIdAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateAuctionByAdminCommandHandler(users.Object, auctions.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.Equal("Updated Title", auction.Title);
        Assert.Equal("Jewellery", auction.Category);
        Assert.Equal("Updated description", auction.Description);
        Assert.Equal(250m, auction.StartingPrice.Amount);
        Assert.Equal("EUR", auction.StartingPrice.Currency);
        Assert.Equal(new[] { "first.png", "second.png" }, auction.Images.OrderBy(x => x.SortOrder).Select(x => x.FileName).ToArray());
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        var command = new UpdateAuctionByAdminCommand(seller.Id, Guid.NewGuid(), "Title", "Art", "Desc", 100m, "USD", DateTime.UtcNow.AddDays(5));

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(seller.Id, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        var handler = new UpdateAuctionByAdminCommandHandler(users.Object, auctions.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuctionNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new UpdateAuctionByAdminCommand(admin.Id, Guid.NewGuid(), "Title", "Art", "Desc", 100m, "USD", DateTime.UtcNow.AddDays(5));

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetWithBidsByIdAsync(command.AuctionId, It.IsAny<CancellationToken>())).ReturnsAsync((Auction?)null);

        var handler = new UpdateAuctionByAdminCommandHandler(users.Object, auctions.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuctionHasEnded_ThrowsInvalidOperationException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var auction = CreateActiveAuction();
        auction.End(DateTime.UtcNow.AddMinutes(-1));
        var command = new UpdateAuctionByAdminCommand(admin.Id, auction.Id, "Title", "Art", "Desc", 100m, "USD", DateTime.UtcNow.AddDays(5));

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetWithBidsByIdAsync(command.AuctionId, It.IsAny<CancellationToken>())).ReturnsAsync(auction);

        var handler = new UpdateAuctionByAdminCommandHandler(users.Object, auctions.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPriceChangesAfterBids_ThrowsInvalidOperationException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var auction = CreateActiveAuction();
        auction.PlaceBid(bidder.Id, Money.Create(150m, "USD"), DateTime.UtcNow.AddMinutes(-1));

        var command = new UpdateAuctionByAdminCommand(admin.Id, auction.Id, "Title", "Art", "Desc", 200m, "USD", DateTime.UtcNow.AddDays(5));

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetWithBidsByIdAsync(command.AuctionId, It.IsAny<CancellationToken>())).ReturnsAsync(auction);

        var handler = new UpdateAuctionByAdminCommandHandler(users.Object, auctions.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Auction CreateActiveAuction()
    {
        var auction = Auction.Create(
            Guid.NewGuid(),
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
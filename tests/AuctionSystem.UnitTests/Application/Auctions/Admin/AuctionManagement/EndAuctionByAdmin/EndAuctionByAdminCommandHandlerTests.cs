using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using MediatR;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin;

public class EndAuctionByAdminCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdminAndActiveAuction_EndsAuctionAndPersists()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var auction = CreateActiveAuction();
        var command = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommand(admin.Id, auction.Id);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommandHandler(
            users.Object,
            auctions.Object,
            unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.Equal(AuctionStatus.Ended, auction.Status);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommand(bidder.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bidder);

        var handler = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommandHandler(
            users.Object,
            auctions.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuctionNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommand(admin.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetByIdAsync(command.AuctionId, It.IsAny<CancellationToken>())).ReturnsAsync((Auction?)null);

        var handler = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommandHandler(
            users.Object,
            auctions.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuctionIsNotActive_ThrowsInvalidOperationException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var auction = Auction.Create(
            Guid.NewGuid(),
            "Draft Auction",
            Money.Create(100m, "USD"),
            DateTime.UtcNow.AddDays(3),
            "Description",
            "General");
        var command = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommand(admin.Id, auction.Id);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);

        var handler = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommandHandler(
            users.Object,
            auctions.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Auction CreateActiveAuction()
    {
        var auction = Auction.Create(
            Guid.NewGuid(),
            "Active Auction",
            Money.Create(100m, "USD"),
            DateTime.UtcNow.AddDays(3),
            "Description",
            "General");
        auction.Start();
        return auction;
    }
}
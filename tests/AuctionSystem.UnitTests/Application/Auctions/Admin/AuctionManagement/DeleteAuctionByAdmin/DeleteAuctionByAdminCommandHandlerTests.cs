using AuctionSystem.Application.Auctions.Admin.AuctionManagement.DeleteAuctionByAdmin;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using MediatR;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement;

public class DeleteAuctionByAdminCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_RemovesAuctionAndPersists()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var auction = CreateActiveAuction();
        var command = new DeleteAuctionByAdminCommand(admin.Id, auction.Id);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteAuctionByAdminCommandHandler(users.Object, auctions.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        auctions.Verify(x => x.Remove(auction), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        var command = new DeleteAuctionByAdminCommand(seller.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(seller.Id, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        var handler = new DeleteAuctionByAdminCommandHandler(users.Object, auctions.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuctionNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new DeleteAuctionByAdminCommand(admin.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        auctions.Setup(x => x.GetByIdAsync(command.AuctionId, It.IsAny<CancellationToken>())).ReturnsAsync((Auction?)null);

        var handler = new DeleteAuctionByAdminCommandHandler(users.Object, auctions.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

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
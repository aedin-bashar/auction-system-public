using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.CreateAuction;

public class CreateAuctionCommandHandlerTests
{
    [Theory]
    [InlineData(UserRole.Seller)]
    [InlineData(UserRole.Admin)]
    public async Task Handle_WithAllowedRole_CreatesStartsAndPersistsAuction(UserRole role)
    {
        var seller = User.Register("seller@example.com", "Seller User", role);
        var command = new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionCommand(
            seller.Id,
            "  Vintage Watch  ",
            "Watches",
            "  Rare collector piece  ",
            250m,
            "USD",
            DateTime.UtcNow.AddDays(7),
            new[]
            {
                new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionImageInput("second.png", "image/png", new byte[] { 2 }, 2),
                new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionImageInput("first.png", "image/png", new byte[] { 1 }, 0)
            });

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        Auction? capturedAuction = null;

        users.Setup(x => x.GetByIdAsync(seller.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seller);
        auctions.Setup(x => x.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>()))
            .Callback<Auction, CancellationToken>((auction, _) => capturedAuction = auction)
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionCommandHandler(
            users.Object,
            auctions.Object,
            unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedAuction);
        Assert.Equal(result, capturedAuction!.Id);
        Assert.Equal(seller.Id, capturedAuction.SellerId);
        Assert.Equal("Vintage Watch", capturedAuction.Title);
        Assert.Equal("Rare collector piece", capturedAuction.Description);
        Assert.Equal(AuctionStatus.Active, capturedAuction.Status);
        Assert.Equal(new[] { 0, 2 }, capturedAuction.Images.Select(x => x.SortOrder).ToArray());
        Assert.Equal(new[] { "first.png", "second.png" }, capturedAuction.Images.Select(x => x.FileName).ToArray());

        auctions.Verify(x => x.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSellerNotFound_ThrowsKeyNotFoundException()
    {
        var command = new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionCommand(
            Guid.NewGuid(),
            "Vintage Watch",
            "Watches",
            "Description",
            250m,
            "USD",
            DateTime.UtcNow.AddDays(7));

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(command.SellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionCommandHandler(
            users.Object,
            auctions.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSellerIsInactive_ThrowsInvalidOperationException()
    {
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        seller.Deactivate();
        var command = new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionCommand(
            seller.Id,
            "Vintage Watch",
            "Watches",
            "Description",
            250m,
            "USD",
            DateTime.UtcNow.AddDays(7));

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(seller.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seller);

        var handler = new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionCommandHandler(
            users.Object,
            auctions.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserRoleIsNotAllowed_ThrowsInvalidOperationException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionCommand(
            bidder.Id,
            "Vintage Watch",
            "Watches",
            "Description",
            250m,
            "USD",
            DateTime.UtcNow.AddDays(7));

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);

        var handler = new AuctionSystem.Application.Auctions.CreateAuction.CreateAuctionCommandHandler(
            users.Object,
            auctions.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
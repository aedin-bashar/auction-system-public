using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.ReportAuction;

public class ReportAuctionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidReporterAndAuction_CreatesReportAndPersists()
    {
        var sellerId = Guid.NewGuid();
        var reporter = User.Register("reporter@example.com", "Reporter User", UserRole.Bidder);
        var auction = Auction.Create(
            sellerId,
            "Collector Camera",
            Money.Create(100m, "USD"),
            DateTime.UtcNow.AddDays(5),
            "Description",
            "Cameras");
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            auction.Id,
            reporter.Id,
            "  Fraudulent listing  ",
            "  Suspicious description  ");
        var caseId = Guid.NewGuid();

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(reporter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reporter);
        auctions.Setup(x => x.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        reports.Setup(x => x.HasOpenCaseAsync(auction.Id, reporter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        reports.Setup(x => x.CreateAsync(
                It.Is<CreateAuctionReportRequest>(r =>
                    r.AuctionId == auction.Id &&
                    r.ReportedByUserId == reporter.Id &&
                    r.Reason == "Fraudulent listing" &&
                    r.Details == "Suspicious description"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseId);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandHandler(
            users.Object,
            auctions.Object,
            reports.Object,
            unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(caseId, result);
        reports.Verify(x => x.CreateAsync(It.IsAny<CreateAuctionReportRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReporterNotFound_ThrowsKeyNotFoundException()
    {
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fraudulent listing",
            null);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(command.ReportedByUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandHandler(
            users.Object,
            auctions.Object,
            reports.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        reports.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReporterIsInactive_ThrowsInvalidOperationException()
    {
        var reporter = User.Register("reporter@example.com", "Reporter User", UserRole.Bidder);
        reporter.Deactivate();
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            Guid.NewGuid(),
            reporter.Id,
            "Fraudulent listing",
            null);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(reporter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reporter);

        var handler = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandHandler(
            users.Object,
            auctions.Object,
            reports.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        auctions.VerifyNoOtherCalls();
        reports.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenAuctionNotFound_ThrowsKeyNotFoundException()
    {
        var reporter = User.Register("reporter@example.com", "Reporter User", UserRole.Bidder);
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            Guid.NewGuid(),
            reporter.Id,
            "Fraudulent listing",
            null);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(reporter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reporter);
        auctions.Setup(x => x.GetByIdAsync(command.AuctionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Auction?)null);

        var handler = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandHandler(
            users.Object,
            auctions.Object,
            reports.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        reports.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReporterOwnsAuction_ThrowsInvalidOperationException()
    {
        var reporter = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        var auction = Auction.Create(
            reporter.Id,
            "Collector Camera",
            Money.Create(100m, "USD"),
            DateTime.UtcNow.AddDays(5),
            "Description",
            "Cameras");
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            auction.Id,
            reporter.Id,
            "Fraudulent listing",
            null);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(reporter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reporter);
        auctions.Setup(x => x.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        var handler = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandHandler(
            users.Object,
            auctions.Object,
            reports.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        reports.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReporterAlreadyHasOpenCase_ThrowsInvalidOperationException()
    {
        var sellerId = Guid.NewGuid();
        var reporter = User.Register("reporter@example.com", "Reporter User", UserRole.Bidder);
        var auction = Auction.Create(
            sellerId,
            "Collector Camera",
            Money.Create(100m, "USD"),
            DateTime.UtcNow.AddDays(5),
            "Description",
            "Cameras");
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            auction.Id,
            reporter.Id,
            "Fraudulent listing",
            null);

        var users = new Mock<IUserRepository>();
        var auctions = new Mock<IAuctionRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(reporter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reporter);
        auctions.Setup(x => x.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        reports.Setup(x => x.HasOpenCaseAsync(auction.Id, reporter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandHandler(
            users.Object,
            auctions.Object,
            reports.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        reports.Verify(x => x.CreateAsync(It.IsAny<CreateAuctionReportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
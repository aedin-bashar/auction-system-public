using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Admin.Moderation;

public class ResolveFlaggedCaseCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ResolvesCaseAndPersists()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var caseId = Guid.NewGuid();
        var command = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommand(
            admin.Id,
            caseId,
            "  Resolved after review  ");
        var resolvedCase = new AdminFlaggedCaseDto(
            caseId,
            Guid.NewGuid(),
            "Auction",
            Guid.NewGuid(),
            "Reporter",
            "Spam",
            "Details",
            "Resolved",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Admin User",
            "Resolved after review");

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        reports.Setup(x => x.ResolveAsync(
                It.Is<ResolveAuctionReportRequest>(r =>
                    r.CaseId == caseId &&
                    r.ResolvedByUserId == admin.Id &&
                    r.ResolutionNote == "Resolved after review"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedCase);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommandHandler(
            users.Object,
            reports.Object,
            unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(resolvedCase, result);
        reports.Verify(x => x.ResolveAsync(It.IsAny<ResolveAuctionReportRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommand(
            bidder.Id,
            Guid.NewGuid(),
            "Resolved");

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);

        var handler = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommandHandler(
            users.Object,
            reports.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        reports.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCaseNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommand(
            admin.Id,
            Guid.NewGuid(),
            "Resolved");

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAuctionReportStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        reports.Setup(x => x.ResolveAsync(It.IsAny<ResolveAuctionReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminFlaggedCaseDto?)null);

        var handler = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommandHandler(
            users.Object,
            reports.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
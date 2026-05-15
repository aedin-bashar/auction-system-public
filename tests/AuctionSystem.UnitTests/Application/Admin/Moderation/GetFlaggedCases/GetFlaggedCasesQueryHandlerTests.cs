using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Application.Admin.Moderation.GetFlaggedCases;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Admin.Moderation;

public class GetFlaggedCasesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsFlaggedCases()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var query = new GetFlaggedCasesQuery(admin.Id, true);
        IReadOnlyList<AdminFlaggedCaseDto> flaggedCases =
        [
            new AdminFlaggedCaseDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Flagged Auction",
                Guid.NewGuid(),
                "Reporter User",
                "Fraud",
                "Suspicious listing",
                "Resolved",
                DateTime.UtcNow.AddDays(-2),
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(-1),
                "Admin User",
                "Closed after review")
        ];

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAuctionReportStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        reports.Setup(x => x.ListAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(flaggedCases);

        var handler = new GetFlaggedCasesQueryHandler(users.Object, reports.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(flaggedCases, result);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var query = new GetFlaggedCasesQuery(bidder.Id, false);

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAuctionReportStore>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bidder);

        var handler = new GetFlaggedCasesQueryHandler(users.Object, reports.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));

        reports.Verify(x => x.ListAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
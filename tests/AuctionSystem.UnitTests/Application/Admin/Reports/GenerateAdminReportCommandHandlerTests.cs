using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Application.Admin.Reports.GenerateAdminReport;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Admin.Reports;

public class GenerateAdminReportCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsReport()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var now = DateTime.UtcNow;
        var report = new AdminReportDto(
            "overview",
            now.AddDays(-7),
            now,
            now,
            new Dictionary<string, decimal> { ["bidVolume"] = 4200m },
            new Dictionary<string, int> { ["bids"] = 15 });

        var command = new GenerateAdminReportCommand(admin.Id, " overview ", now.AddDays(-7), now);

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        reports.Setup(x => x.GenerateAsync(It.IsAny<GenerateAdminReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var handler = new GenerateAdminReportCommandHandler(users.Object, reports.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(report, result);
        reports.Verify(x => x.GenerateAsync(
            It.Is<GenerateAdminReportRequest>(r =>
                r.ReportType == "overview" &&
                r.RangeStartUtc == command.RangeStartUtc &&
                r.RangeEndUtc == command.RangeEndUtc &&
                r.RequestedByUserId == admin.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new GenerateAdminReportCommand(bidder.Id, "overview", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);

        var handler = new GenerateAdminReportCommandHandler(users.Object, reports.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
        reports.Verify(x => x.GenerateAsync(It.IsAny<GenerateAdminReportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

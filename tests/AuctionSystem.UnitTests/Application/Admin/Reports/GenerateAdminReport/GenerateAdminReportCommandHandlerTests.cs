using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Application.Admin.Reports.GenerateAdminReport;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Admin.Reports.GenerateAdminReport;

public class GenerateAdminReportCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsGeneratedReport()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new GenerateAdminReportCommand(admin.Id, "  overview  ", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        var report = new AdminReportDto(
            "overview",
            command.RangeStartUtc,
            command.RangeEndUtc,
            DateTime.UtcNow,
            new Dictionary<string, decimal> { ["bidVolume"] = 250m },
            new Dictionary<string, int> { ["bids"] = 2 });

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        reports.Setup(x => x.GenerateAsync(
                It.Is<GenerateAdminReportRequest>(r =>
                    r.ReportType == "overview" &&
                    r.RequestedByUserId == admin.Id &&
                    r.RangeStartUtc == command.RangeStartUtc &&
                    r.RangeEndUtc == command.RangeEndUtc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var handler = new GenerateAdminReportCommandHandler(users.Object, reports.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(report, result);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new GenerateAdminReportCommand(bidder.Id, "overview", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bidder);

        var handler = new GenerateAdminReportCommandHandler(users.Object, reports.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
        reports.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenReportGenerationReturnsNull_ThrowsInvalidOperationException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new GenerateAdminReportCommand(admin.Id, "overview", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        reports.Setup(x => x.GenerateAsync(It.IsAny<GenerateAdminReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminReportDto?)null);

        var handler = new GenerateAdminReportCommandHandler(users.Object, reports.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
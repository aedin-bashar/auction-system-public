using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Application.Admin.Reports.GetAdminDashboard;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Admin.Reports;

public class GetAdminDashboardQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsDashboard()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var dashboard = new AdminDashboardDto(
            DateTime.UtcNow,
            5,
            7,
            9,
            1,
            Array.Empty<AdminDashboardActivityDto>());

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        reports.Setup(x => x.GetDashboardAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        var handler = new GetAdminDashboardQueryHandler(users.Object, reports.Object);

        var result = await handler.Handle(new GetAdminDashboardQuery(admin.Id), CancellationToken.None);

        Assert.Equal(dashboard, result);
        reports.Verify(x => x.GetDashboardAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);

        var handler = new GetAdminDashboardQueryHandler(users.Object, reports.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new GetAdminDashboardQuery(bidder.Id), CancellationToken.None));
        reports.Verify(x => x.GetDashboardAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Application.Admin.Reports.GetAdminDashboard;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Admin.Reports.GetAdminDashboard;

public class GetAdminDashboardQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsDashboard()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var query = new GetAdminDashboardQuery(admin.Id);
        var dashboard = new AdminDashboardDto(DateTime.UtcNow, 3, 2, 5, 1, []);

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        reports.Setup(x => x.GetDashboardAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(dashboard);

        var handler = new GetAdminDashboardQueryHandler(users.Object, reports.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(dashboard, result);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller);
        var query = new GetAdminDashboardQuery(seller.Id);

        var users = new Mock<IUserRepository>();
        var reports = new Mock<IAdminReportStore>();

        users.Setup(x => x.GetByIdAsync(seller.Id, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        var handler = new GetAdminDashboardQueryHandler(users.Object, reports.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));
        reports.VerifyNoOtherCalls();
    }
}
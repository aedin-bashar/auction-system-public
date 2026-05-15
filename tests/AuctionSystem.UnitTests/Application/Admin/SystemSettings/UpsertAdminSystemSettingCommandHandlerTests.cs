using AuctionSystem.Application.Admin.SystemSettings;
using AuctionSystem.Application.Admin.SystemSettings.UpsertAdminSystemSetting;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Admin.SystemSettings;

public class UpsertAdminSystemSettingCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_UpsertsAndReturnsSetting()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var setting = new AdminSystemSettingDto("maintenance.mode", "false", DateTime.UtcNow, admin.Id);
        var command = new UpsertAdminSystemSettingCommand(admin.Id, " maintenance.mode ", " false ");

        var users = new Mock<IUserRepository>();
        var settings = new Mock<IAdminSystemSettingsStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        settings.Setup(x => x.UpsertAsync(It.IsAny<UpsertAdminSystemSettingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);

        var handler = new UpsertAdminSystemSettingCommandHandler(users.Object, settings.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(setting, result);
        settings.Verify(x => x.UpsertAsync(
            It.Is<UpsertAdminSystemSettingRequest>(r =>
                r.Key == "maintenance.mode" &&
                r.Value == "false" &&
                r.UpdatedByUserId == admin.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new UpsertAdminSystemSettingCommand(bidder.Id, "maintenance.mode", "false");

        var users = new Mock<IUserRepository>();
        var settings = new Mock<IAdminSystemSettingsStore>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);

        var handler = new UpsertAdminSystemSettingCommandHandler(users.Object, settings.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
        settings.Verify(x => x.UpsertAsync(It.IsAny<UpsertAdminSystemSettingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

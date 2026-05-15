using AuctionSystem.Application.Users.Admin.UserManagement.GetAdminUsers;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.Admin.UserManagement;

public class GetAdminUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsUsersOrderedByActiveThenCreatedAt()
    {
        var now = DateTime.UtcNow;
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin, nowUtc: now.AddHours(-4));
        var activeOlder = User.Register("active-older@example.com", "Active Older", UserRole.Bidder, nowUtc: now.AddHours(-3));
        var activeNewer = User.Register("active-newer@example.com", "Active Newer", UserRole.Seller, nowUtc: now.AddHours(-1));
        var inactiveUser = User.Register("inactive@example.com", "Inactive User", UserRole.Bidder, nowUtc: now.AddHours(-2));
        inactiveUser.Deactivate(now.AddMinutes(-30));

        var query = new GetAdminUsersQuery(admin.Id);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        users.Setup(x => x.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { inactiveUser, activeOlder, activeNewer, admin });

        var handler = new GetAdminUsersQueryHandler(users.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(4, result.Count);
        Assert.Equal(activeNewer.Id, result[0].UserId);
        Assert.Equal(activeOlder.Id, result[1].UserId);
        Assert.Equal(admin.Id, result[2].UserId);
        Assert.Equal(inactiveUser.Id, result[3].UserId);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var query = new GetAdminUsersQuery(bidder.Id);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bidder);

        var handler = new GetAdminUsersQueryHandler(users.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));

        users.Verify(x => x.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
using AuctionSystem.Application.Users.Profile.GetUserProfile;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.Profile;

public class GetUserProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingUser_ReturnsMappedProfileDto()
    {
        var user = User.Register("user@example.com", "Profile User", UserRole.Seller, "+1 555 111 2222");
        var query = new GetUserProfileQuery(user.Id);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new GetUserProfileQueryHandler(users.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Equal(user.PhoneNumber, result.PhoneNumber);
        Assert.Equal(user.Role.ToString(), result.Role);
        Assert.Equal(user.IsActive, result.IsActive);
        Assert.Equal(user.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(user.UpdatedAtUtc, result.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var query = new GetUserProfileQuery(Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new GetUserProfileQueryHandler(users.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}
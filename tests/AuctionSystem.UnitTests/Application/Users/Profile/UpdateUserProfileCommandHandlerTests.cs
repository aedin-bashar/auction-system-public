using AuctionSystem.Application.Users.Profile.UpdateUserProfile;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.Profile;

public class UpdateUserProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidData_UpdatesUserAndReturnsProfileDto()
    {
        var user = User.Register("john@example.com", "John Doe", phoneNumber: "+1 111 111 1111");
        var command = new UpdateUserProfileCommand(user.Id, "jane@example.com", "Jane Doe", "+1 222 222 2222");

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateUserProfileCommandHandler(users.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("jane@example.com", result.Email);
        Assert.Equal("Jane Doe", result.FullName);
        Assert.Equal("+1 222 222 2222", result.PhoneNumber);
        Assert.Equal(user.Role.ToString(), result.Role);
        Assert.True(result.IsActive);
        users.Verify(x => x.Update(It.Is<User>(u => u.Id == user.Id)), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var command = new UpdateUserProfileCommand(Guid.NewGuid(), "john@example.com", "John Doe", null);

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new UpdateUserProfileCommandHandler(users.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        users.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailUnchanged_DoesNotThrowAndPersistsProfileUpdate()
    {
        var user = User.Register("john@example.com", "John Doe", phoneNumber: "+1 111 111 1111");
        var command = new UpdateUserProfileCommand(user.Id, "  JOHN@EXAMPLE.COM  ", "John Updated", "+1 333 333 3333");

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new UpdateUserProfileCommandHandler(users.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("John Updated", result.FullName);
        Assert.Equal("+1 333 333 3333", result.PhoneNumber);
        users.Verify(x => x.Update(It.Is<User>(u => u.Id == user.Id)), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

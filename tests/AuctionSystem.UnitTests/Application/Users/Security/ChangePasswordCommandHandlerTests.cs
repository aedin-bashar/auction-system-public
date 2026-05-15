using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Users.Security.ChangePassword;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.Security;

public class ChangePasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveUserAndValidCurrentPassword_UpdatesPasswordAndPersists()
    {
        var user = User.Register("user@example.com", "Valid User", UserRole.Bidder);
        var command = new ChangePasswordCommand(user.Id, "Current123!", "NewSecret123!");

        var users = new Mock<IUserRepository>();
        var passwordVerifier = new Mock<IPasswordVerifier>();
        var passwordStore = new Mock<IPasswordStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordVerifier.Setup(x => x.VerifyAsync(user.Id, command.CurrentPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ChangePasswordCommandHandler(
            users.Object,
            passwordVerifier.Object,
            passwordStore.Object,
            unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        passwordStore.Verify(
            x => x.SetPasswordAsync(user.Id, command.NewPassword, It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        var command = new ChangePasswordCommand(Guid.NewGuid(), "Current123!", "NewSecret123!");

        var users = new Mock<IUserRepository>();
        var passwordVerifier = new Mock<IPasswordVerifier>();
        var passwordStore = new Mock<IPasswordStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new ChangePasswordCommandHandler(
            users.Object,
            passwordVerifier.Object,
            passwordStore.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        passwordVerifier.VerifyNoOtherCalls();
        passwordStore.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ThrowsUnauthorizedAccessException()
    {
        var user = User.Register("inactive@example.com", "Inactive User", UserRole.Bidder);
        user.Deactivate();
        var command = new ChangePasswordCommand(user.Id, "Current123!", "NewSecret123!");

        var users = new Mock<IUserRepository>();
        var passwordVerifier = new Mock<IPasswordVerifier>();
        var passwordStore = new Mock<IPasswordStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ChangePasswordCommandHandler(
            users.Object,
            passwordVerifier.Object,
            passwordStore.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        passwordVerifier.VerifyNoOtherCalls();
        passwordStore.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordIsInvalid_ThrowsUnauthorizedAccessException()
    {
        var user = User.Register("user@example.com", "Valid User", UserRole.Bidder);
        var command = new ChangePasswordCommand(user.Id, "Wrong123!", "NewSecret123!");

        var users = new Mock<IUserRepository>();
        var passwordVerifier = new Mock<IPasswordVerifier>();
        var passwordStore = new Mock<IPasswordStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordVerifier.Setup(x => x.VerifyAsync(user.Id, command.CurrentPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new ChangePasswordCommandHandler(
            users.Object,
            passwordVerifier.Object,
            passwordStore.Object,
            unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        passwordStore.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
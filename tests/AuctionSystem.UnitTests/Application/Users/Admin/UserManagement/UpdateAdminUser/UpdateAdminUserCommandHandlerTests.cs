using AuctionSystem.Application.Users.Admin.UserManagement.UpdateAdminUser;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.Admin.UserManagement;

public class UpdateAdminUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidAdminUpdate_ChangesTargetUserAndPersists()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var target = User.Register("target@example.com", "Target User", UserRole.Bidder, "+1 555 111 1111");
        var command = new UpdateAdminUserCommand(
            admin.Id,
            target.Id,
            "updated@example.com",
            "Updated User",
            "+1 555 222 2222",
            "Seller",
            false);

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        users.Setup(x => x.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateAdminUserCommandHandler(users.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(target.Id, result.UserId);
        Assert.Equal("updated@example.com", result.Email);
        Assert.Equal("Updated User", result.FullName);
        Assert.Equal("Seller", result.Role);
        Assert.False(result.IsActive);
        users.Verify(x => x.Update(target), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new UpdateAdminUserCommand(bidder.Id, Guid.NewGuid(), "user@example.com", "User", null, "Bidder", true);

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bidder);

        var handler = new UpdateAdminUserCommandHandler(users.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetUserNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new UpdateAdminUserCommand(admin.Id, Guid.NewGuid(), "user@example.com", "User", null, "Bidder", true);

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        users.Setup(x => x.GetByIdAsync(command.TargetUserId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = new UpdateAdminUserCommandHandler(users.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyInUse_ThrowsInvalidOperationException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var target = User.Register("target@example.com", "Target User", UserRole.Bidder);
        var other = User.Register("other@example.com", "Other User", UserRole.Bidder);
        var command = new UpdateAdminUserCommand(admin.Id, target.Id, other.Email, "Target User", null, "Bidder", true);

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        users.Setup(x => x.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(other);

        var handler = new UpdateAdminUserCommandHandler(users.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAdminAttemptsToDeactivateSelf_ThrowsInvalidOperationException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new UpdateAdminUserCommand(admin.Id, admin.Id, admin.Email, admin.FullName, admin.PhoneNumber, "Admin", false);

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        var handler = new UpdateAdminUserCommandHandler(users.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
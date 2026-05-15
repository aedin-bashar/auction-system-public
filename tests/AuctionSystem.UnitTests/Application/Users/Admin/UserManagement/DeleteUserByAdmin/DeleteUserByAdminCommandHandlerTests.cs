using AuctionSystem.Application.Users.Admin.UserManagement.DeleteUserByAdmin;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.Admin.UserManagement;

public class DeleteUserByAdminCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_RemovesTargetUserAndPersists()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var target = User.Register("target@example.com", "Target User", UserRole.Bidder);
        var command = new DeleteUserByAdminCommand(admin.Id, target.Id);

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        users.Setup(x => x.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteUserByAdminCommandHandler(users.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        users.Verify(x => x.Remove(target), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotActiveAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new DeleteUserByAdminCommand(bidder.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bidder);

        var handler = new DeleteUserByAdminCommandHandler(users.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAdminDeletesSelf_ThrowsInvalidOperationException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new DeleteUserByAdminCommand(admin.Id, admin.Id);

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        var handler = new DeleteUserByAdminCommandHandler(users.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetUserNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new DeleteUserByAdminCommand(admin.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        users.Setup(x => x.GetByIdAsync(command.TargetUserId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = new DeleteUserByAdminCommandHandler(users.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
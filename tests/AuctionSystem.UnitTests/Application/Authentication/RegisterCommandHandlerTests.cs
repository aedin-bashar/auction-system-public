using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Authentication.Models;
using AuctionSystem.Application.Authentication.Register;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Authentication;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidData_ReturnsLoginResult()
    {
        var command = new RegisterCommand("john@example.com", "Secret123!", "John Doe", null);
        var token = new TokenResult("token", new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc));

        var users = new Mock<IUserRepository>();
        var passwordStore = new Mock<IPasswordStore>();
        var tokenService = new Mock<ITokenService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        users.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        passwordStore.Setup(x => x.SetPasswordAsync(It.IsAny<Guid>(), command.Password, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        tokenService.Setup(x => x.CreateAccessTokenAsync(It.IsAny<TokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = new RegisterCommandHandler(users.Object, passwordStore.Object, tokenService.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal(token.AccessToken, result.AccessToken);
        users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        passwordStore.Verify(x => x.SetPasswordAsync(It.IsAny<Guid>(), command.Password, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        tokenService.Verify(x => x.CreateAccessTokenAsync(It.IsAny<TokenRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ThrowsInvalidOperationException()
    {
        var existing = User.Register("john@example.com", "John Doe");
        var command = new RegisterCommand("john@example.com", "Secret123!", "John Doe", null);

        var users = new Mock<IUserRepository>();
        var passwordStore = new Mock<IPasswordStore>();
        var tokenService = new Mock<ITokenService>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new RegisterCommandHandler(users.Object, passwordStore.Object, tokenService.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        users.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        passwordStore.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
        tokenService.VerifyNoOtherCalls();
    }
}

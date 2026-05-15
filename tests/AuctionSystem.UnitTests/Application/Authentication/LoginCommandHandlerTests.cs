using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Authentication.Login;
using AuctionSystem.Application.Authentication.Models;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Authentication;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsLoginResult()
    {
        var user = User.Register("john@example.com", "John Doe");
        var command = new LoginCommand("john@example.com", "Secret123!");
        var token = new TokenResult("token", new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc));

        var users = new Mock<IUserRepository>();
        var passwordVerifier = new Mock<IPasswordVerifier>();
        var tokenService = new Mock<ITokenService>();

        users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordVerifier.Setup(x => x.VerifyAsync(user.Id, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        tokenService.Setup(x => x.CreateAccessTokenAsync(
                It.Is<TokenRequest>(r => r.UserId == user.Id && r.Email == user.Email && r.Role == user.Role.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = new LoginCommandHandler(
            users.Object,
            passwordVerifier.Object,
            tokenService.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Equal(user.Role.ToString(), result.Role);
        Assert.Equal(token.AccessToken, result.AccessToken);
        Assert.Equal(token.ExpiresAtUtc, result.ExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        var command = new LoginCommand("missing@example.com", "Secret123!");

        var users = new Mock<IUserRepository>();
        var passwordVerifier = new Mock<IPasswordVerifier>();
        var tokenService = new Mock<ITokenService>();

        users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new LoginCommandHandler(
            users.Object,
            passwordVerifier.Object,
            tokenService.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        passwordVerifier.VerifyNoOtherCalls();
        tokenService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ThrowsUnauthorizedAccessException()
    {
        var user = User.Register("john@example.com", "John Doe");
        user.Deactivate();
        var command = new LoginCommand("john@example.com", "Secret123!");

        var users = new Mock<IUserRepository>();
        var passwordVerifier = new Mock<IPasswordVerifier>();
        var tokenService = new Mock<ITokenService>();

        users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LoginCommandHandler(
            users.Object,
            passwordVerifier.Object,
            tokenService.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        passwordVerifier.VerifyNoOtherCalls();
        tokenService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ThrowsUnauthorizedAccessException()
    {
        var user = User.Register("john@example.com", "John Doe");
        var command = new LoginCommand("john@example.com", "Secret123!");

        var users = new Mock<IUserRepository>();
        var passwordVerifier = new Mock<IPasswordVerifier>();
        var tokenService = new Mock<ITokenService>();

        users.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordVerifier.Setup(x => x.VerifyAsync(user.Id, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new LoginCommandHandler(
            users.Object,
            passwordVerifier.Object,
            tokenService.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        tokenService.VerifyNoOtherCalls();
    }
}

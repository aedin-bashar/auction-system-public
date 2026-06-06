using System.Net;
using System.Net.Http.Json;
using AuctionSystem.API.Controllers;
using AuctionSystem.Application.Authentication.Login;
using AuctionSystem.Application.Authentication.Models;
using AuctionSystem.Application.Authentication.Register;
using Moq;
using Xunit.Sdk;

namespace AuctionSystem.IntegrationTests.Auth;

public class AuthControllerTests
{
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new XunitException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {body}");
    }

    [Fact]
    public async Task RegisterAndLogin_WithValidData_ReturnsSuccess()
    {
        await using var factory = new CustomWebApplicationFactory();

        var token = new TokenResult("test-token", new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc));
        factory.TokenServiceMock
            .Setup(x => x.CreateAccessTokenAsync(It.IsAny<TokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        factory.PasswordVerifierMock
            .Setup(x => x.VerifyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        factory.PasswordStoreMock
            .Setup(x => x.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = factory.CreateClient();

        var register = new RegisterCommand("john@example.com", "Secret123!", "John Doe", null);
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", register);
        await EnsureSuccessAsync(registerResponse);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<LoginResultDto>();
        Assert.NotNull(registerResult);
        Assert.Equal("john@example.com", registerResult!.Email);
        Assert.Equal("John Doe", registerResult.FullName);
        Assert.Equal(token.AccessToken, registerResult.AccessToken);

        var login = new LoginCommand("john@example.com", "Secret123!");
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", login);
        await EnsureSuccessAsync(loginResponse);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();
        Assert.NotNull(loginResult);
        Assert.Equal("john@example.com", loginResult!.Email);
        Assert.Equal(token.AccessToken, loginResult.AccessToken);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsFailure()
    {
        await using var factory = new CustomWebApplicationFactory();

        var token = new TokenResult("test-token", new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc));
        factory.TokenServiceMock
            .Setup(x => x.CreateAccessTokenAsync(It.IsAny<TokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        factory.PasswordStoreMock
            .Setup(x => x.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = factory.CreateClient();

        var register = new RegisterCommand("john@example.com", "Secret123!", "John Doe", null);
        var firstResponse = await client.PostAsJsonAsync("/api/auth/register", register);
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", register);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.False(secondResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsFailure()
    {
        await using var factory = new CustomWebApplicationFactory();

        var token = new TokenResult("test-token", new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc));
        factory.TokenServiceMock
            .Setup(x => x.CreateAccessTokenAsync(It.IsAny<TokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        factory.PasswordVerifierMock
            .Setup(x => x.VerifyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        factory.PasswordStoreMock
            .Setup(x => x.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = factory.CreateClient();

        var register = new RegisterCommand("john@example.com", "Secret123!", "John Doe", null);
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", register);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var login = new LoginCommand("john@example.com", "WrongPassword!");
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", login);

        Assert.False(loginResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithSubPathReferer_SendsResetLinkUnderSubPath()
    {
        await using var factory = new CustomWebApplicationFactory();

        var token = new TokenResult("test-token", new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc));
        factory.TokenServiceMock
            .Setup(x => x.CreateAccessTokenAsync(It.IsAny<TokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        factory.PasswordStoreMock
            .Setup(x => x.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        string? emailBody = null;
        factory.EmailSenderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => emailBody = body)
            .Returns(Task.CompletedTask);

        var client = factory.CreateClient();

        var register = new RegisterCommand("john@example.com", "Secret123!", "John Doe", null);
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", register);
        await EnsureSuccessAsync(registerResponse);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/forgot-password")
        {
            Content = JsonContent.Create(new ForgotPasswordRequest("john@example.com"))
        };
        request.Headers.Referrer = new Uri("http://localhost/projects/auctions/forgot-password");

        var response = await client.SendAsync(request);

        await EnsureSuccessAsync(response);
        Assert.NotNull(emailBody);
        Assert.Contains("http://localhost/projects/auctions/reset-password?token=", emailBody);
    }
}

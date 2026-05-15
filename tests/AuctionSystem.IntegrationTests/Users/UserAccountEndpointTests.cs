using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Application.Users.Profile;
using AuctionSystem.Domain.Users;
using AuctionSystem.IntegrationTests.Auth;
using AuctionSystem.IntegrationTests.Infrastructure;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AuctionSystem.IntegrationTests.Users;

public sealed class UserAccountEndpointTests
{
    [Fact]
    public async Task GetProfile_WithAuthenticatedUser_ReturnsProfile()
    {
        await using var factory = new CustomWebApplicationFactory();
        var userId = await SeedUserAsync(factory, "profile@example.com", "Profile User", UserRole.Bidder, "+1 555 111 1111");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(userId, "profile@example.com", UserRole.Bidder.ToString()));

        var response = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal(userId, profile!.UserId);
        Assert.Equal("profile@example.com", profile.Email);
        Assert.Equal("Profile User", profile.FullName);
        Assert.Equal("+1 555 111 1111", profile.PhoneNumber);
    }

    [Fact]
    public async Task UpdateProfile_WithAuthenticatedUser_PersistsChanges()
    {
        await using var factory = new CustomWebApplicationFactory();
        var userId = await SeedUserAsync(factory, "old@example.com", "Old Name", UserRole.Bidder, "+1 555 111 1111");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(userId, "old@example.com", UserRole.Bidder.ToString()));

        var response = await client.PutAsJsonAsync("/api/users/profile", new
        {
            Email = "new@example.com",
            FullName = "New Name",
            PhoneNumber = "+1 555 222 2222"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("new@example.com", profile!.Email);
        Assert.Equal("New Name", profile.FullName);
        Assert.Equal("+1 555 222 2222", profile.PhoneNumber);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedUser = await db.Users.FindAsync(userId);

        Assert.NotNull(updatedUser);
        Assert.Equal("new@example.com", updatedUser!.Email);
        Assert.Equal("New Name", updatedUser.FullName);
        Assert.Equal("+1 555 222 2222", updatedUser.PhoneNumber);
    }

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_ReturnsNoContentAndStoresNewPassword()
    {
        await using var factory = new CustomWebApplicationFactory();
        var userId = await SeedUserAsync(factory, "security@example.com", "Security User", UserRole.Bidder, null);

        factory.PasswordVerifierMock
            .Setup(x => x.VerifyAsync(userId, "Current123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        factory.PasswordStoreMock
            .Setup(x => x.SetPasswordAsync(userId, "NewSecret123!", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(userId, "security@example.com", UserRole.Bidder.ToString()));

        var response = await client.PostAsJsonAsync("/api/users/security/change-password", new
        {
            CurrentPassword = "Current123!",
            NewPassword = "NewSecret123!"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        factory.PasswordStoreMock.Verify(
            x => x.SetPasswordAsync(userId, "NewSecret123!", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        var userId = await SeedUserAsync(factory, "security@example.com", "Security User", UserRole.Bidder, null);

        factory.PasswordVerifierMock
            .Setup(x => x.VerifyAsync(userId, "WrongPassword!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(userId, "security@example.com", UserRole.Bidder.ToString()));

        var response = await client.PostAsJsonAsync("/api/users/security/change-password", new
        {
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewSecret123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        factory.PasswordStoreMock.Verify(
            x => x.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static async Task<Guid> SeedUserAsync(
        CustomWebApplicationFactory factory,
        string email,
        string fullName,
        UserRole role,
        string? phoneNumber)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = User.Register(email, fullName, role, phoneNumber);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }
}
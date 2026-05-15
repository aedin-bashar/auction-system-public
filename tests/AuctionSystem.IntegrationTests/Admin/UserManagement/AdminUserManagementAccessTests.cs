using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Domain.Users;
using AuctionSystem.IntegrationTests.Infrastructure;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionSystem.IntegrationTests.Admin.UserManagement;

public class AdminUserManagementAccessTests
{
    [Fact]
    public async Task GetUsers_WhenRequesterIsAdmin_ReturnsOk()
    {
        await using var factory = new AdminUserManagementWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WhenRequesterIsNotAdmin_ReturnsForbidden()
    {
        await using var factory = new AdminUserManagementWebApplicationFactory();
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenRequesterIsAdmin_UpdatesTargetSuccessfully()
    {
        await using var factory = new AdminUserManagementWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var targetId = await factory.SeedUserAsync("target@example.com", "Target User", UserRole.Seller);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var request = new
        {
            email = "updated.target@example.com",
            fullName = "Updated Target",
            phoneNumber = "+1 333 333 3333",
            role = "Bidder",
            isActive = true
        };

        var response = await client.PutAsJsonAsync($"/api/admin/users/{targetId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await db.Users.FindAsync(targetId);

        Assert.NotNull(updated);
        Assert.Equal("updated.target@example.com", updated!.Email);
        Assert.Equal("Updated Target", updated.FullName);
        Assert.Equal(UserRole.Bidder, updated.Role);
    }

    [Fact]
    public async Task DeleteUser_WhenRequesterIsAdmin_DeletesTargetSuccessfully()
    {
        await using var factory = new AdminUserManagementWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var targetId = await factory.SeedUserAsync("target@example.com", "Target User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.DeleteAsync($"/api/admin/users/{targetId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deleted = await db.Users.FindAsync(targetId);
        Assert.Null(deleted);
    }
}

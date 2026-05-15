using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Domain.Users;
using AuctionSystem.IntegrationTests.Infrastructure;

namespace AuctionSystem.IntegrationTests.Admin.ReportsAndSettings;

public class AdminReportsAndSettingsAccessTests
{
    [Fact]
    public async Task GenerateReport_WhenRequesterIsAdmin_ReturnsOk()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.PostAsJsonAsync("/api/admin/reports/generate", new
        {
            reportType = "overview",
            rangeStartUtc = DateTime.UtcNow.AddDays(-30),
            rangeEndUtc = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_WhenRequesterIsAdmin_ReturnsOk()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.GetAsync("/api/admin/reports/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListFlaggedCases_WhenRequesterIsAdmin_ReturnsOk()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.GetAsync("/api/admin/moderation/cases");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GenerateReport_WhenRequesterIsNotAdmin_ReturnsForbidden()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.PostAsJsonAsync("/api/admin/reports/generate", new
        {
            reportType = "overview",
            rangeStartUtc = DateTime.UtcNow.AddDays(-30),
            rangeEndUtc = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_WhenRequesterIsNotAdmin_ReturnsForbidden()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.GetAsync("/api/admin/reports/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListFlaggedCases_WhenRequesterIsNotAdmin_ReturnsForbidden()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.GetAsync("/api/admin/moderation/cases");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResolveFlaggedCase_WhenRequesterIsNotAdmin_ReturnsForbidden()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.PostAsJsonAsync($"/api/admin/moderation/cases/{Guid.NewGuid()}/resolve", new
        {
            resolutionNote = "Not allowed"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpsertSetting_WhenRequesterIsAdmin_ReturnsOk()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.PutAsJsonAsync("/api/admin/settings/maintenance.mode", new
        {
            value = "true"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpsertSetting_WhenRequesterIsNotAdmin_ReturnsForbidden()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.PutAsJsonAsync("/api/admin/settings/maintenance.mode", new
        {
            value = "true"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Headers;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.IntegrationTests.Infrastructure;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionSystem.IntegrationTests.Admin.AuctionManagement;

public class AdminAuctionManagementAccessTests
{
    [Fact]
    public async Task GetAuctions_WhenRequesterIsAdmin_ReturnsOk()
    {
        await using var factory = new AdminAuctionManagementWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var sellerId = await factory.SeedUserAsync("seller@example.com", "Seller User", UserRole.Seller);
        await factory.SeedAuctionAsync(sellerId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.GetAsync("/api/admin/auctions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAuctions_WhenRequesterIsNotAdmin_ReturnsForbidden()
    {
        await using var factory = new AdminAuctionManagementWebApplicationFactory();
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.GetAsync("/api/admin/auctions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EndAuction_WhenRequesterIsAdmin_EndsAuctionSuccessfully()
    {
        await using var factory = new AdminAuctionManagementWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var sellerId = await factory.SeedUserAsync("seller@example.com", "Seller User", UserRole.Seller);
        var auctionId = await factory.SeedAuctionAsync(sellerId, title: "Active Auction");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.PostAsync($"/api/admin/auctions/{auctionId}/end", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await db.Auctions.FindAsync(auctionId);

        Assert.NotNull(updated);
        Assert.Equal(AuctionStatus.Ended, updated!.Status);
        Assert.NotNull(updated.EndedAtUtc);
    }

    [Fact]
    public async Task GetAuctionDetail_WhenRequesterIsAdmin_ReturnsOk()
    {
        await using var factory = new AdminAuctionManagementWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var sellerId = await factory.SeedUserAsync("seller@example.com", "Seller User", UserRole.Seller);
        var auctionId = await factory.SeedAuctionAsync(sellerId, title: "Detail Auction");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.GetAsync($"/api/admin/auctions/{auctionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuction_WhenRequesterIsAdmin_DeletesAuctionSuccessfully()
    {
        await using var factory = new AdminAuctionManagementWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var sellerId = await factory.SeedUserAsync("seller@example.com", "Seller User", UserRole.Seller);
        var auctionId = await factory.SeedAuctionAsync(sellerId, title: "Delete Auction");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.DeleteAsync($"/api/admin/auctions/{auctionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deleted = await db.Auctions.FindAsync(auctionId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task UpdateAuction_WhenRequesterIsAdmin_UpdatesAuctionSuccessfully()
    {
        await using var factory = new AdminAuctionManagementWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var sellerId = await factory.SeedUserAsync("seller@example.com", "Seller User", UserRole.Seller);
        var auctionId = await factory.SeedAuctionAsync(sellerId, title: "Old Auction", startingPriceAmount: 100m, currency: "USD");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Updated Auction"), "Title");
        content.Add(new StringContent("Art"), "Category");
        content.Add(new StringContent("Updated description"), "Description");
        content.Add(new StringContent("150"), "StartingPriceAmount");
        content.Add(new StringContent("EUR"), "Currency");
        content.Add(new StringContent(DateTime.UtcNow.AddDays(7).ToString("O")), "EndTimeUtc");
        content.Add(new StringContent("true"), "ReplaceImages");

        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "Images", "updated.png");

        var response = await client.PutAsync($"/api/admin/auctions/{auctionId}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await db.Auctions
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == auctionId);

        Assert.Equal("Updated Auction", updated.Title);
        Assert.Equal("Art", updated.Category);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(150m, updated.StartingPrice.Amount);
        Assert.Equal("EUR", updated.StartingPrice.Currency);
        Assert.Single(updated.Images);
        Assert.Equal("updated.png", updated.Images.Single().FileName);
    }
}

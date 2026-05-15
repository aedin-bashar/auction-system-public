using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Application.Auctions.GetActiveAuctions;
using AuctionSystem.Application.Auctions.MyBids;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using AuctionSystem.IntegrationTests.Bidding;
using AuctionSystem.IntegrationTests.Infrastructure;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionSystem.IntegrationTests.Auctions;

public sealed class AuctionEndpointTests
{
    [Fact]
    public async Task GetActiveAuctions_WithFilters_ReturnsMatchingAuctions()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var sellerId = await SeedUserAsync(factory, "seller@example.com", "Seller User", UserRole.Seller);

        await SeedAuctionAsync(factory, sellerId, "Collectible Watch", "Collectibles", 250m, "USD", startAuction: true);
        await SeedAuctionAsync(factory, sellerId, "Gaming Console", "Electronics", 400m, "USD", startAuction: true);
        await SeedAuctionAsync(factory, sellerId, "Draft Listing", "Collectibles", 275m, "USD", startAuction: false);

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auctions?category=Collectibles&minPrice=200&maxPrice=300&pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auctions = await response.Content.ReadFromJsonAsync<IReadOnlyList<GetActiveAuctionsItemDto>>();
        Assert.NotNull(auctions);
        Assert.Single(auctions!);
        Assert.Equal("Collectible Watch", auctions[0].Title);
        Assert.Equal(250m, auctions[0].PriceAmount);
    }

    [Fact]
    public async Task GetAuctionImage_WithExistingImage_ReturnsFileContent()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var sellerId = await SeedUserAsync(factory, "seller@example.com", "Seller User", UserRole.Seller);
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        var (auctionId, imageId) = await SeedAuctionWithImageAsync(factory, sellerId, imageBytes, "image/png");

        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/auctions/{auctionId}/images/{imageId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(imageBytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task GetMyBids_WithAuthenticatedBidder_ReturnsOwnedBids()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var sellerId = await SeedUserAsync(factory, "seller@example.com", "Seller User", UserRole.Seller);
        var bidderId = await SeedUserAsync(factory, "bidder@example.com", "Bidder User", UserRole.Bidder);
        await SeedAuctionWithBidAsync(factory, sellerId, bidderId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.GetAsync("/api/auctions/my-bids");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bids = await response.Content.ReadFromJsonAsync<IReadOnlyList<MyBidItemDto>>();
        Assert.NotNull(bids);
        Assert.Single(bids!);
        Assert.Equal("Collector Watch", bids[0].Title);
        Assert.Equal(150m, bids[0].MyMaxBidAmount);
        Assert.Equal(150m, bids[0].CurrentHighestBidAmount);
    }

    [Fact]
    public async Task CreateAuction_WithJsonRequest_CreatesAuction()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var sellerId = await SeedUserAsync(factory, "seller@example.com", "Seller User", UserRole.Seller);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(sellerId, "seller@example.com", UserRole.Seller.ToString()));

        var response = await client.PostAsJsonAsync("/api/auctions", new
        {
            Title = "Vintage Camera",
            Category = "Cameras",
            Description = "Film camera in working condition",
            StartingPriceAmount = 125m,
            Currency = "USD",
            EndTimeUtc = DateTime.UtcNow.AddDays(5)
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auctionId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, auctionId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auction = await db.Auctions.Include(x => x.Images).SingleAsync(x => x.Id == auctionId);

        Assert.Equal("Vintage Camera", auction.Title);
        Assert.Equal("Cameras", auction.Category);
        Assert.Equal(125m, auction.StartingPrice.Amount);
        Assert.Empty(auction.Images);
    }

    [Fact]
    public async Task CreateAuctionWithImage_WithMultipartRequest_CreatesAuctionAndStoresImage()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var sellerId = await SeedUserAsync(factory, "seller@example.com", "Seller User", UserRole.Seller);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(sellerId, "seller@example.com", UserRole.Seller.ToString()));

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Vintage Poster"), "Title");
        content.Add(new StringContent("Art"), "Category");
        content.Add(new StringContent("Framed poster"), "Description");
        content.Add(new StringContent("95"), "StartingPriceAmount");
        content.Add(new StringContent("USD"), "Currency");
        content.Add(new StringContent(DateTime.UtcNow.AddDays(7).ToString("O")), "EndTimeUtc");

        var imageContent = new ByteArrayContent(new byte[] { 9, 8, 7, 6 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "Images", "poster.png");

        var response = await client.PostAsync("/api/auctions", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auctionId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, auctionId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auction = await db.Auctions.Include(x => x.Images).SingleAsync(x => x.Id == auctionId);

        Assert.Single(auction.Images);
        Assert.Equal("poster.png", auction.Images.Single().FileName);
        Assert.Equal("image/png", auction.Images.Single().ContentType);
    }

    private static async Task<Guid> SeedUserAsync(BiddingWebApplicationFactory factory, string email, string fullName, UserRole role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = User.Register(email, fullName, role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    private static async Task<Guid> SeedAuctionAsync(
        BiddingWebApplicationFactory factory,
        Guid sellerId,
        string title,
        string category,
        decimal startingPriceAmount,
        string currency,
        bool startAuction)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var auction = Auction.Create(
            sellerId,
            title,
            Money.Create(startingPriceAmount, currency),
            DateTime.UtcNow.AddDays(5),
            "Seeded description",
            category,
            DateTime.UtcNow.AddHours(-1));

        if (startAuction)
        {
            auction.Start(DateTime.UtcNow.AddMinutes(-30));
        }

        db.Auctions.Add(auction);
        await db.SaveChangesAsync();

        return auction.Id;
    }

    private static async Task<(Guid AuctionId, Guid ImageId)> SeedAuctionWithImageAsync(
        BiddingWebApplicationFactory factory,
        Guid sellerId,
        byte[] imageBytes,
        string contentType)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var auction = Auction.Create(
            sellerId,
            "Image Auction",
            Money.Create(100m, "USD"),
            DateTime.UtcNow.AddDays(5),
            "Seeded description",
            "Art",
            DateTime.UtcNow.AddHours(-1));

        auction.Start(DateTime.UtcNow.AddMinutes(-30));
        var image = auction.AddImage("image.png", contentType, imageBytes, 0, DateTime.UtcNow.AddMinutes(-20));

        db.Auctions.Add(auction);
        await db.SaveChangesAsync();

        return (auction.Id, image.Id);
    }

    private static async Task SeedAuctionWithBidAsync(BiddingWebApplicationFactory factory, Guid sellerId, Guid bidderId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var auction = Auction.Create(
            sellerId,
            "Collector Watch",
            Money.Create(100m, "USD"),
            DateTime.UtcNow.AddDays(5),
            "Seeded description",
            "Watches",
            DateTime.UtcNow.AddHours(-1));

        auction.Start(DateTime.UtcNow.AddMinutes(-30));
        auction.AddImage("watch.png", "image/png", new byte[] { 1, 2, 3 }, 0, DateTime.UtcNow.AddMinutes(-25));
        auction.PlaceBid(bidderId, Money.Create(150m, "USD"), DateTime.UtcNow.AddMinutes(-10));

        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
    }
}
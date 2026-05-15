using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Application.Auctions.PlaceBid;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.IntegrationTests.Infrastructure;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AuctionSystem.IntegrationTests.Bidding;

public sealed class PlaceBidEndpointTests
{
    [Fact]
    public async Task PlaceBid_WithValidRequest_UpdatesAuctionAndCreatesBid()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var client = factory.CreateClient();

        var (_, bidderId, auctionId) = await factory.SeedActiveAuctionAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var command = new
        {
            Amount = 150m,
            Currency = "USD"
        };

        var response = await client.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", command);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PlaceBidResultDto>();
        Assert.NotNull(result);
        Assert.Equal(auctionId, result!.AuctionId);
        Assert.Equal(bidderId, result.BidderId);
        Assert.Equal(150m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(150m, result.CurrentPriceAmount);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var auction = await db.Auctions
            .Include(x => x.Bids)
            .SingleAsync(x => x.Id == auctionId);

        Assert.Equal(AuctionStatus.Active, auction.Status);
        Assert.Single(auction.Bids);

        var bid = auction.Bids.Single();
        Assert.Equal(bidderId, bid.BidderId);
        Assert.Equal(150m, bid.Amount.Amount);
        Assert.Equal("USD", bid.Amount.Currency);
        Assert.Equal(150m, auction.CurrentPrice.Amount);

        factory.RealtimeNotifierMock.Verify(
            x => x.NotifyBidPlacedAsync(It.IsAny<AuctionSystem.Application.Abstractions.Realtime.BidPlacedRealtimeEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PlaceBid_WithAmountNotGreaterThanCurrentPrice_ReturnsFailureAndDoesNotCreateBid()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var client = factory.CreateClient();

        var (_, bidderId, auctionId) = await factory.SeedActiveAuctionAsync(startingPriceAmount: 100m, currency: "USD");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var command = new
        {
            Amount = 100m,
            Currency = "USD"
        };

        var response = await client.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", command);

        Assert.False(response.IsSuccessStatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var auction = await db.Auctions
            .Include(x => x.Bids)
            .SingleAsync(x => x.Id == auctionId);

        Assert.Empty(auction.Bids);
        Assert.Equal(100m, auction.CurrentPrice.Amount);

        factory.RealtimeNotifierMock.Verify(
            x => x.NotifyBidPlacedAsync(It.IsAny<AuctionSystem.Application.Abstractions.Realtime.BidPlacedRealtimeEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PlaceBid_WithConcurrentRequests_PersistsConsistentAuctionState()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var firstClient = factory.CreateClient();
        var secondClient = factory.CreateClient();

        var (_, bidderId, auctionId) = await factory.SeedActiveAuctionAsync(startingPriceAmount: 100m, currency: "USD");
        var token = TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString());

        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var firstRequest = new { Amount = 150m, Currency = "USD" };
        var secondRequest = new { Amount = 160m, Currency = "USD" };

        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", firstRequest),
            secondClient.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", secondRequest));

        var successfulResults = new List<PlaceBidResultDto>();
        foreach (var response in responses)
        {
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            var result = await response.Content.ReadFromJsonAsync<PlaceBidResultDto>();
            Assert.NotNull(result);
            successfulResults.Add(result!);
        }

        Assert.NotEmpty(successfulResults);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var auction = await db.Auctions
            .Include(x => x.Bids)
            .SingleAsync(x => x.Id == auctionId);

        Assert.Equal(successfulResults.Count, auction.Bids.Count);
        Assert.Equal(successfulResults.Max(x => x.Amount), auction.CurrentPrice.Amount);

        foreach (var successfulResult in successfulResults)
        {
            Assert.Contains(auction.Bids, bid =>
                bid.BidderId == successfulResult.BidderId &&
                bid.Amount.Amount == successfulResult.Amount &&
                bid.Amount.Currency == successfulResult.Currency);
        }

        factory.RealtimeNotifierMock.Verify(
            x => x.NotifyBidPlacedAsync(It.IsAny<AuctionSystem.Application.Abstractions.Realtime.BidPlacedRealtimeEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(successfulResults.Count));
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Channels;
using AuctionSystem.Application.Abstractions.Realtime;
using AuctionSystem.Domain.Users;
using AuctionSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace AuctionSystem.IntegrationTests.Bidding;

public sealed class AuctionHubRealtimeTests
{
    [Fact]
    public async Task JoinedClient_ReceivesBidPlacedEvent_FromBidEndpoint()
    {
        await using var factory = new BiddingWebApplicationFactory(useMockRealtimeNotifier: false);
        var client = factory.CreateClient();

        var (_, bidderId, auctionId) = await factory.SeedActiveAuctionAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var events = Channel.CreateUnbounded<BidPlacedRealtimeEvent>();
        await using var connection = CreateConnection(factory);
        connection.On<BidPlacedRealtimeEvent>("BidPlaced", bidEvent => events.Writer.TryWrite(bidEvent));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinAuction", auctionId);

        var response = await client.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", new
        {
            Amount = 150m,
            Currency = "USD"
        });

        response.EnsureSuccessStatusCode();

        var bidEvent = await ReadEventAsync(events.Reader, TimeSpan.FromSeconds(5));

        Assert.Equal(auctionId, bidEvent.AuctionId);
        Assert.Equal(bidderId, bidEvent.BidderId);
        Assert.Equal(150m, bidEvent.Amount);
        Assert.Equal("USD", bidEvent.Currency);
        Assert.Equal(150m, bidEvent.CurrentPriceAmount);
    }

    [Fact]
    public async Task ClientThatLeavesAuction_DoesNotReceiveSubsequentBidPlacedEvent()
    {
        await using var factory = new BiddingWebApplicationFactory(useMockRealtimeNotifier: false);
        var client = factory.CreateClient();

        var (_, bidderId, auctionId) = await factory.SeedActiveAuctionAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var events = Channel.CreateUnbounded<BidPlacedRealtimeEvent>();
        await using var connection = CreateConnection(factory);
        connection.On<BidPlacedRealtimeEvent>("BidPlaced", bidEvent => events.Writer.TryWrite(bidEvent));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinAuction", auctionId);

        var firstResponse = await client.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", new
        {
            Amount = 150m,
            Currency = "USD"
        });

        firstResponse.EnsureSuccessStatusCode();
        _ = await ReadEventAsync(events.Reader, TimeSpan.FromSeconds(5));

        await connection.InvokeAsync("LeaveAuction", auctionId);

        var secondResponse = await client.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", new
        {
            Amount = 160m,
            Currency = "USD"
        });

        secondResponse.EnsureSuccessStatusCode();

        var receivedAfterLeave = await TryReadEventAsync(events.Reader, TimeSpan.FromSeconds(1));
        Assert.Null(receivedAfterLeave);
    }

    private static HubConnection CreateConnection(BiddingWebApplicationFactory factory)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/auctions"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();
    }

    private static async Task<BidPlacedRealtimeEvent> ReadEventAsync(ChannelReader<BidPlacedRealtimeEvent> reader, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return await reader.ReadAsync(cts.Token);
    }

    private static async Task<BidPlacedRealtimeEvent?> TryReadEventAsync(ChannelReader<BidPlacedRealtimeEvent> reader, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            return await reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
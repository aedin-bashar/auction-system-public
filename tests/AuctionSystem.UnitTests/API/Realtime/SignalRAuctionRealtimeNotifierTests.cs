using AuctionSystem.API.Hubs;
using AuctionSystem.API.Realtime;
using AuctionSystem.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace AuctionSystem.UnitTests.API.Realtime;

public class SignalRAuctionRealtimeNotifierTests
{
    [Fact]
    public async Task NotifyBidPlacedAsync_WithValidEvent_SendsToAuctionGroup()
    {
        var bidEvent = new BidPlacedRealtimeEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            150m,
            "USD",
            DateTime.UtcNow,
            150m,
            "USD");

        var hubContext = new Mock<IHubContext<AuctionHub>>();
        var clients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();

        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);
        clients.Setup(x => x.Group(AuctionHub.GroupName(bidEvent.AuctionId))).Returns(clientProxy.Object);
        clientProxy
            .Setup(x => x.SendCoreAsync(
                "BidPlaced",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], bidEvent)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notifier = new SignalRAuctionRealtimeNotifier(hubContext.Object);

        await notifier.NotifyBidPlacedAsync(bidEvent, CancellationToken.None);

        clientProxy.Verify(
            x => x.SendCoreAsync(
                "BidPlaced",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], bidEvent)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyBidPlacedAsync_WhenEventNull_ThrowsArgumentNullException()
    {
        var hubContext = new Mock<IHubContext<AuctionHub>>();
        var notifier = new SignalRAuctionRealtimeNotifier(hubContext.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => notifier.NotifyBidPlacedAsync(null!, CancellationToken.None));
    }

    [Fact]
    public void Constructor_WhenHubContextNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRAuctionRealtimeNotifier(null!));
    }
}
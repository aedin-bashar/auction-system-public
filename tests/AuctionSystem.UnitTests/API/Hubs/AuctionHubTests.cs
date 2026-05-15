using AuctionSystem.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace AuctionSystem.UnitTests.API.Hubs;

public class AuctionHubTests
{
    [Fact]
    public void GroupName_ReturnsExpectedGroupName()
    {
        var auctionId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var result = AuctionHub.GroupName(auctionId);

        Assert.Equal("auction-11111111-2222-3333-4444-555555555555", result);
    }

    [Fact]
    public async Task JoinAuction_WithValidAuctionId_AddsConnectionToAuctionGroup()
    {
        var auctionId = Guid.NewGuid();
        var context = new Mock<HubCallerContext>();
        var groups = new Mock<IGroupManager>();

        context.SetupGet(x => x.ConnectionId).Returns("connection-1");
        groups.Setup(x => x.AddToGroupAsync("connection-1", AuctionHub.GroupName(auctionId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new AuctionHub
        {
            Context = context.Object,
            Groups = groups.Object
        };

        await hub.JoinAuction(auctionId);

        groups.Verify(
            x => x.AddToGroupAsync("connection-1", AuctionHub.GroupName(auctionId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveAuction_WithValidAuctionId_RemovesConnectionFromAuctionGroup()
    {
        var auctionId = Guid.NewGuid();
        var context = new Mock<HubCallerContext>();
        var groups = new Mock<IGroupManager>();

        context.SetupGet(x => x.ConnectionId).Returns("connection-1");
        groups.Setup(x => x.RemoveFromGroupAsync("connection-1", AuctionHub.GroupName(auctionId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new AuctionHub
        {
            Context = context.Object,
            Groups = groups.Object
        };

        await hub.LeaveAuction(auctionId);

        groups.Verify(
            x => x.RemoveFromGroupAsync("connection-1", AuctionHub.GroupName(auctionId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinAuction_WithEmptyAuctionId_ThrowsHubException()
    {
        var hub = new AuctionHub();

        var exception = await Assert.ThrowsAsync<HubException>(() => hub.JoinAuction(Guid.Empty));

        Assert.Equal("AuctionId is required.", exception.Message);
    }

    [Fact]
    public async Task LeaveAuction_WithEmptyAuctionId_ThrowsHubException()
    {
        var hub = new AuctionHub();

        var exception = await Assert.ThrowsAsync<HubException>(() => hub.LeaveAuction(Guid.Empty));

        Assert.Equal("AuctionId is required.", exception.Message);
    }
}
using Microsoft.AspNetCore.SignalR;

namespace AuctionSystem.API.Hubs;

public sealed class AuctionHub : Hub
{
    public Task JoinAuction(Guid auctionId)
    {
        if (auctionId == Guid.Empty)
        {
            throw new HubException("AuctionId is required.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(auctionId));
    }

    public Task LeaveAuction(Guid auctionId)
    {
        if (auctionId == Guid.Empty)
        {
            throw new HubException("AuctionId is required.");
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(auctionId));
    }

    public static string GroupName(Guid auctionId) => $"auction-{auctionId:D}";
}
using AuctionSystem.API.Hubs;
using AuctionSystem.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace AuctionSystem.API.Realtime;

public sealed class SignalRAuctionRealtimeNotifier : IAuctionRealtimeNotifier
{
    private readonly IHubContext<AuctionHub> _hubContext;

    public SignalRAuctionRealtimeNotifier(IHubContext<AuctionHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public Task NotifyBidPlacedAsync(BidPlacedRealtimeEvent bidEvent, CancellationToken cancellationToken = default)
    {
        if (bidEvent is null) throw new ArgumentNullException(nameof(bidEvent));

        return _hubContext
            .Clients
            .Group(AuctionHub.GroupName(bidEvent.AuctionId))
            .SendAsync("BidPlaced", bidEvent, cancellationToken);
    }
}
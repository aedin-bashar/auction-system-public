namespace AuctionSystem.Application.Abstractions.Realtime;

public interface IAuctionRealtimeNotifier
{
    Task NotifyBidPlacedAsync(BidPlacedRealtimeEvent bidEvent, CancellationToken cancellationToken = default);
}

public sealed record BidPlacedRealtimeEvent(
    Guid AuctionId,
    Guid BidId,
    Guid BidderId,
    decimal Amount,
    string Currency,
    DateTime PlacedAtUtc,
    decimal CurrentPriceAmount,
    string CurrentPriceCurrency);
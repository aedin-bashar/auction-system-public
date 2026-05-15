using MediatR;

namespace AuctionSystem.Application.Auctions.PlaceBid;

public sealed record PlaceBidCommand(
    Guid AuctionId,
    Guid BidderId,
    decimal Amount,
    string Currency) : IRequest<PlaceBidResultDto>;

public sealed record PlaceBidResultDto(
    Guid BidId,
    Guid AuctionId,
    Guid BidderId,
    decimal Amount,
    string Currency,
    DateTime PlacedAtUtc,
    decimal CurrentPriceAmount,
    string CurrentPriceCurrency);
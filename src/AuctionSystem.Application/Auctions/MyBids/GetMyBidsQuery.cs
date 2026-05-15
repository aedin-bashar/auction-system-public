using MediatR;

namespace AuctionSystem.Application.Auctions.MyBids;

public sealed record GetMyBidsQuery(Guid BidderId) : IRequest<IReadOnlyList<MyBidItemDto>>;

public sealed record MyBidItemDto(
    Guid AuctionId,
    string Title,
    string Category,
    decimal MyMaxBidAmount,
    decimal CurrentHighestBidAmount,
    string Currency,
    int BidCount,
    DateTime EndTimeUtc,
    Guid? PrimaryImageId);

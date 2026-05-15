using MediatR;

namespace AuctionSystem.Application.Auctions.GetActiveAuctions;

public sealed record GetActiveAuctionsQuery(
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<IReadOnlyList<GetActiveAuctionsItemDto>>;

public sealed record GetActiveAuctionsItemDto(
    Guid Id,
    Guid SellerId,
    string Title,
    string Category,
    string? Description,
    decimal PriceAmount,
    string Currency,
    DateTime EndTimeUtc,
    int BidCount,
    Guid? PrimaryImageId = null);

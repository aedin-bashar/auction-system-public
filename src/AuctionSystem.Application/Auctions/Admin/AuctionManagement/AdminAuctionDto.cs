namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement;

public sealed record AdminAuctionListItemDto(
    Guid AuctionId,
    string Title,
    Guid SellerId,
    string SellerName,
    string Category,
    decimal CurrentBidAmount,
    string Currency,
    int BidCount,
    DateTime EndTimeUtc,
    string Status);

public sealed record AdminAuctionBidDto(
    Guid BidId,
    Guid BidderId,
    string BidderName,
    decimal Amount,
    string Currency,
    DateTime PlacedAtUtc);

public sealed record AdminAuctionDetailDto(
    Guid AuctionId,
    string Title,
    Guid SellerId,
    string SellerName,
    string Category,
    string? Description,
    decimal StartingPriceAmount,
    string Currency,
    decimal CurrentBidAmount,
    int BidCount,
    DateTime? StartTimeUtc,
    DateTime EndTimeUtc,
    DateTime? EndedAtUtc,
    string Status,
    string? HighestBidderName,
    Guid? PrimaryImageId,
    int ImageCount,
    IReadOnlyList<AdminAuctionBidDto> Bids);

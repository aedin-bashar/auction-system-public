using MediatR;

namespace AuctionSystem.Application.Auctions.CreateAuction;

public sealed record CreateAuctionImageInput(
    string FileName,
    string ContentType,
    byte[] Content,
    int SortOrder = 0);

public sealed record CreateAuctionCommand(
    Guid SellerId,
    string Title,
    string Category,
    string? Description,
    decimal StartingPriceAmount,
    string Currency,
    DateTime EndTimeUtc,
    IReadOnlyList<CreateAuctionImageInput>? Images = null) : IRequest<Guid>;

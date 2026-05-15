using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.UpdateAuctionByAdmin;

public sealed record UpdateAuctionImageInput(
    string FileName,
    string ContentType,
    byte[] Content,
    int SortOrder = 0);

public sealed record UpdateAuctionByAdminCommand(
    Guid RequesterUserId,
    Guid AuctionId,
    string Title,
    string Category,
    string? Description,
    decimal StartingPriceAmount,
    string Currency,
    DateTime EndTimeUtc,
    bool ReplaceImages = false,
    IReadOnlyList<UpdateAuctionImageInput>? Images = null) : IRequest<Unit>;

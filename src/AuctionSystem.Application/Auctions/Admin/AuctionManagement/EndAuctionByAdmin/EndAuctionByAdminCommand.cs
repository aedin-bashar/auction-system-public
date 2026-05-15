using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin;

public sealed record EndAuctionByAdminCommand(
    Guid RequesterUserId,
    Guid AuctionId) : IRequest<Unit>;
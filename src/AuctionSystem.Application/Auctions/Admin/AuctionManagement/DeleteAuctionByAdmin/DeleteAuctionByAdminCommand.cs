using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.DeleteAuctionByAdmin;

public sealed record DeleteAuctionByAdminCommand(
    Guid RequesterUserId,
    Guid AuctionId) : IRequest<Unit>;
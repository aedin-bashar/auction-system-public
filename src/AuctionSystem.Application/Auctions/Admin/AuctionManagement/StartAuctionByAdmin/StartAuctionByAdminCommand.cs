using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.StartAuctionByAdmin;

public sealed record StartAuctionByAdminCommand(
    Guid RequesterUserId,
    Guid AuctionId) : IRequest<Unit>;

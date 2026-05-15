using AuctionSystem.Application.Auctions.Admin.AuctionManagement;
using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctionDetail;

public sealed record GetAdminAuctionDetailQuery(Guid RequesterUserId, Guid AuctionId) : IRequest<AdminAuctionDetailDto>;

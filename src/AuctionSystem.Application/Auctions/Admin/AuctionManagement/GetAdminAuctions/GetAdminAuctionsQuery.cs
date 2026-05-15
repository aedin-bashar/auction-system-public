using AuctionSystem.Application.Auctions.Admin.AuctionManagement;
using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctions;

public sealed record GetAdminAuctionsQuery(Guid RequesterUserId) : IRequest<IReadOnlyList<AdminAuctionListItemDto>>;

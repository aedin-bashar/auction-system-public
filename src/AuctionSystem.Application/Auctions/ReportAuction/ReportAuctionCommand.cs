using MediatR;

namespace AuctionSystem.Application.Auctions.ReportAuction;

public sealed record ReportAuctionCommand(
    Guid AuctionId,
    Guid ReportedByUserId,
    string Reason,
    string? Details) : IRequest<Guid>;
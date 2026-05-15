using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using MediatR;

namespace AuctionSystem.Application.Auctions.MyBids;

public sealed class GetMyBidsQueryHandler : IRequestHandler<GetMyBidsQuery, IReadOnlyList<MyBidItemDto>>
{
    private readonly IAuctionRepository _auctions;

    public GetMyBidsQueryHandler(IAuctionRepository auctions)
    {
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
    }

    public async Task<IReadOnlyList<MyBidItemDto>> Handle(GetMyBidsQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (request.BidderId == Guid.Empty) throw new ArgumentException("BidderId is required.", nameof(request.BidderId));

        var nowUtc = DateTime.UtcNow;
        var activeAuctions = await _auctions.ListAsync(
            auction => auction.Status == AuctionStatus.Active && auction.EndTimeUtc > nowUtc,
            cancellationToken);

        return activeAuctions
            .Where(auction => auction.Bids.Any(bid => bid.BidderId == request.BidderId))
            .Select(auction =>
            {
                var myMaxBid = auction.Bids
                    .Where(bid => bid.BidderId == request.BidderId)
                    .Max(bid => bid.Amount.Amount);

                return new MyBidItemDto(
                    auction.Id,
                    auction.Title,
                    auction.Category,
                    myMaxBid,
                    auction.CurrentPrice.Amount,
                    auction.CurrentPrice.Currency,
                    auction.Bids.Count,
                    auction.EndTimeUtc,
                    auction.Images
                        .OrderBy(img => img.SortOrder)
                        .ThenBy(img => img.CreatedAtUtc)
                        .Select(img => (Guid?)img.Id)
                        .FirstOrDefault());
            })
            .OrderBy(item => item.EndTimeUtc)
            .ToList();
    }
}

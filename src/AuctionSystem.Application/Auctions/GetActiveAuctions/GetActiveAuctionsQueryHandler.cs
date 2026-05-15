using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Auctions.GetActiveAuctions;

public sealed class GetActiveAuctionsQueryHandler : IRequestHandler<GetActiveAuctionsQuery, IReadOnlyList<GetActiveAuctionsItemDto>>
{
    private readonly IAuctionRepository _auctions;

    public GetActiveAuctionsQueryHandler(IAuctionRepository auctions)
    {
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
    }

    public async Task<IReadOnlyList<GetActiveAuctionsItemDto>> Handle(GetActiveAuctionsQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var activeAuctions = await _auctions.ListActiveAsync(
            request.Category,
            request.MinPrice,
            request.MaxPrice,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return activeAuctions
            .Select(auction => new GetActiveAuctionsItemDto(
                auction.Id,
                auction.SellerId,
                auction.Title,
                auction.Category,
                auction.Description,
                auction.CurrentPrice.Amount,
                auction.CurrentPrice.Currency,
                auction.EndTimeUtc,
                auction.Bids.Count,
                auction.Images
                    .OrderBy(img => img.SortOrder)
                    .ThenBy(img => img.CreatedAtUtc)
                    .Select(img => (Guid?)img.Id)
                    .FirstOrDefault()))
            .ToList();
    }
}

using AuctionSystem.Domain.Auctions;

namespace AuctionSystem.Domain.Abstractions;

public interface IAuctionRepository : IRepository<Auction, Guid>
{
    /// <summary>
    /// Returns a single page of active auctions with optional category and price filters applied in persistence.
    /// </summary>
    Task<IReadOnlyList<Auction>> ListActiveAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an Auction aggregate with its Bid collection populated (if persistence supports it).
    /// </summary>
    Task<Auction?> GetWithBidsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an Auction aggregate with its Image collection populated (if persistence supports it).
    /// </summary>
    Task<Auction?> GetWithImagesByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

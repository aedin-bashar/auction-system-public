using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AuctionSystem.Infrastructure.Repositories;

public sealed class AuctionRepository : EfRepository<Auction, Guid>, IAuctionRepository
{
    private readonly ApplicationDbContext _db;

    public AuctionRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Auction>> ListActiveAsync(
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
        }

        var nowUtc = DateTime.UtcNow;
        var normalizedCategory = NormalizeCategory(category);

        IQueryable<Auction> query = _db.Auctions
            .AsNoTracking()
            .Where(auction => auction.Status == AuctionStatus.Active && auction.EndTimeUtc > nowUtc);

        if (!string.IsNullOrWhiteSpace(normalizedCategory))
        {
            query = query.Where(auction => auction.Category == normalizedCategory);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(auction =>
                (auction.Bids.Select(bid => (decimal?)bid.Amount.Amount).Max() ?? auction.StartingPrice.Amount) >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(auction =>
                (auction.Bids.Select(bid => (decimal?)bid.Amount.Amount).Max() ?? auction.StartingPrice.Amount) <= maxPrice.Value);
        }

        var skip = (pageNumber - 1) * pageSize;

        return await query
            .OrderBy(auction => auction.EndTimeUtc)
            .ThenBy(auction => auction.Id)
            .Skip(skip)
            .Take(pageSize)
            .Include(auction => auction.Bids)
            .Include(auction => auction.Images)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public Task<Auction?> GetWithBidsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        // Include the public navigation using a lambda so EF resolves the mapped navigation.
        // Use AsSplitQuery to avoid cartesian explosion with multiple collections
        return _db.Auctions
            .Include(a => a.Bids)
            .Include(a => a.Images)
            .AsSplitQuery()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Auction?> GetWithImagesByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        return _db.Auctions
            .Include(a => a.Images)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public override async Task<IReadOnlyList<Auction>> ListAsync(
        Expression<Func<Auction, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        // Auctions frequently need CurrentPrice/BidsCount, so include bids for list queries.
        // Use AsSplitQuery to avoid cartesian explosion with multiple collections
        return await _db.Auctions
            .Include(a => a.Bids)
            .Include(a => a.Images)
            .AsSplitQuery()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    private static string? NormalizeCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

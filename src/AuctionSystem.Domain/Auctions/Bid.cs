using AuctionSystem.Domain.ValueObjects;

namespace AuctionSystem.Domain.Auctions;

public sealed class Bid
{
    private Bid(Guid id, Guid auctionId, Guid bidderId, Money amount, DateTime placedAtUtc)
    {
        Id = id;
        AuctionId = auctionId;
        BidderId = bidderId;
        Amount = amount;
        PlacedAtUtc = placedAtUtc;
    }

    // For ORM materialization
    private Bid() { }

    public Guid Id { get; private set; }
    public Guid AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTime PlacedAtUtc { get; private set; }

    public static Bid Create(Guid auctionId, Guid bidderId, Money amount, DateTime? placedAtUtc = null)
    {
        if (auctionId == Guid.Empty)
        {
            throw new ArgumentException("AuctionId is required.", nameof(auctionId));
        }

        if (bidderId == Guid.Empty)
        {
            throw new ArgumentException("BidderId is required.", nameof(bidderId));
        }

        if (amount is null)
        {
            throw new ArgumentNullException(nameof(amount));
        }

        if (amount.Amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Bid amount must be greater than zero.");
        }

        var when = EnsureUtcOrDefault(placedAtUtc);
        return new Bid(Guid.NewGuid(), auctionId, bidderId, amount, when);
    }

    private static DateTime EnsureUtcOrDefault(DateTime? dtUtc)
    {
        var dt = dtUtc ?? DateTime.UtcNow;

        if (dt.Kind == DateTimeKind.Local)
        {
            return dt.ToUniversalTime();
        }

        if (dt.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        return dt;
    }
}
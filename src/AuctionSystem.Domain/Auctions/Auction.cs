using AuctionSystem.Domain.ValueObjects;

namespace AuctionSystem.Domain.Auctions;

public sealed class Auction
{
    private const decimal MinBidIncrement = 1m;
    private readonly List<Bid> _bids = new();
    private readonly List<AuctionImage> _images = new();

    private Auction(
        Guid id,
        Guid sellerId,
        string title,
        string category,
        string? description,
        Money startingPrice,
        DateTime endTimeUtc,
        DateTime nowUtc)
    {
        Id = id;
        SellerId = sellerId;
        Title = title;
        Category = category;
        Description = description;
        StartingPrice = startingPrice;
        Status = AuctionStatus.Draft;

        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        EndTimeUtc = endTimeUtc;
    }

    private Auction() { }

    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Category { get; private set; } = "General";
    public string? Description { get; private set; }

    public Money StartingPrice { get; private set; } = Money.Create(0m, "USD"); // overwritten by ctor
    public AuctionStatus Status { get; private set; }

    public DateTime? StartTimeUtc { get; private set; }
    public DateTime EndTimeUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();
    public IReadOnlyCollection<AuctionImage> Images => _images.AsReadOnly();

    public Money CurrentPrice
    {
        get
        {
            if (_bids.Count == 0) return StartingPrice;
            return _bids.MaxBy(b => b.Amount.Amount)!.Amount;
        }
    }

    public static Auction Create(
        Guid sellerId,
        string title,
        Money startingPrice,
        DateTime endTimeUtc,
        string? description = null,
        string? category = null,
        DateTime? nowUtc = null)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("SellerId is required.", nameof(sellerId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length < 3 || normalizedTitle.Length > 120)
        {
            throw new ArgumentOutOfRangeException(nameof(title), "Title must be between 3 and 120 characters.");
        }

        var normalizedCategory = NormalizeCategory(category);
        var now = EnsureUtcOrDefault(nowUtc);
        var endUtc = EnsureUtc(endTimeUtc);

        if (endUtc <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(endTimeUtc), "End time must be in the future.");
        }

        return new Auction(
            Guid.NewGuid(),
            sellerId,
            normalizedTitle,
            normalizedCategory,
            description?.Trim(),
            startingPrice,
            endUtc,
            now);
    }

    public void Start(DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);

        if (Status != AuctionStatus.Draft)
        {
            throw new InvalidOperationException("Only draft auctions can be started.");
        }

        if (EndTimeUtc <= now)
        {
            throw new InvalidOperationException("Cannot start an auction that has already ended.");
        }

        Status = AuctionStatus.Active;
        StartTimeUtc = now;
        UpdatedAtUtc = now;
    }

    public Bid PlaceBid(Guid bidderId, Money amount, DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);

        if (Status != AuctionStatus.Active)
        {
            throw new InvalidOperationException("Bids can only be placed on active auctions.");
        }

        if (bidderId == Guid.Empty)
        {
            throw new ArgumentException("BidderId is required.", nameof(bidderId));
        }

        if (bidderId == SellerId)
        {
            throw new InvalidOperationException("Seller cannot bid on their own auction.");
        }

        if (now >= EndTimeUtc)
        {
            throw new InvalidOperationException("Cannot place a bid after the auction end time.");
        }

        if (!string.Equals(amount.Currency, StartingPrice.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Bid currency must match auction currency.");
        }

        var minAllowed = CurrentPrice.Amount + MinBidIncrement;
        if (amount.Amount < minAllowed)
        {
            throw new InvalidOperationException("Bid amount must be at least 1.00 greater than the current price.");
        }

        var bid = Bid.Create(Id, bidderId, amount, now);
        _bids.Add(bid);

        UpdatedAtUtc = now;
        return bid;
    }

    public void End(DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);

        if (Status == AuctionStatus.Ended)
        {
            throw new InvalidOperationException("Auction is already ended.");
        }

        if (Status != AuctionStatus.Active)
        {
            throw new InvalidOperationException("Only active auctions can be ended.");
        }

        Status = AuctionStatus.Ended;
        EndedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public AuctionImage AddImage(
        string fileName,
        string contentType,
        byte[] content,
        int sortOrder = 0,
        DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);
        var image = AuctionImage.Create(Id, fileName, contentType, content, sortOrder, now);
        _images.Add(image);
        UpdatedAtUtc = now;
        return image;
    }

    public void ReplaceImages(
        IReadOnlyList<(string FileName, string ContentType, byte[] Content)> images,
        DateTime? nowUtc = null)
    {
        if (images is null)
        {
            throw new ArgumentNullException(nameof(images));
        }

        var now = EnsureUtcOrDefault(nowUtc);
        _images.Clear();

        for (var index = 0; index < images.Count; index++)
        {
            var image = images[index];
            _images.Add(AuctionImage.Create(Id, image.FileName, image.ContentType, image.Content, index, now));
        }

        UpdatedAtUtc = now;
    }

    public void UpdateDetails(
        string title,
        string category,
        string? description,
        DateTime endTimeUtc,
        DateTime? nowUtc = null)
    {
        if (Status == AuctionStatus.Ended)
        {
            throw new InvalidOperationException("Ended auctions cannot be edited.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length < 3 || normalizedTitle.Length > 120)
        {
            throw new ArgumentOutOfRangeException(nameof(title), "Title must be between 3 and 120 characters.");
        }

        var normalizedCategory = NormalizeCategory(category);
        var now = EnsureUtcOrDefault(nowUtc);
        var endUtc = EnsureUtc(endTimeUtc);

        if (endUtc <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(endTimeUtc), "End time must be in the future.");
        }

        Title = normalizedTitle;
        Category = normalizedCategory;
        Description = description?.Trim();
        EndTimeUtc = endUtc;
        UpdatedAtUtc = now;
    }

    public void UpdateStartingPrice(Money price, DateTime? nowUtc = null)
    {
        if (price is null)
        {
            throw new ArgumentNullException(nameof(price));
        }

        if (_bids.Count > 0)
        {
            throw new InvalidOperationException("Starting price cannot be changed after bids are placed.");
        }

        if (Status == AuctionStatus.Ended)
        {
            throw new InvalidOperationException("Ended auctions cannot be edited.");
        }

        StartingPrice = price;
        UpdatedAtUtc = EnsureUtcOrDefault(nowUtc);
    }

    private static DateTime EnsureUtcOrDefault(DateTime? nowUtc)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        return EnsureUtc(now);
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
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

    private static string NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return "General";
        }

        var normalizedCategory = category.Trim();
        if (normalizedCategory.Length < 2 || normalizedCategory.Length > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(category), "Category must be between 2 and 50 characters.");
        }

        return normalizedCategory;
    }

}

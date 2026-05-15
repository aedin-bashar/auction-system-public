namespace AuctionSystem.Domain.Auctions;

public sealed class AuctionImage
{
    private AuctionImage(
        Guid id,
        Guid auctionId,
        string fileName,
        string contentType,
        byte[] content,
        int sortOrder,
        DateTime createdAtUtc)
    {
        Id = id;
        AuctionId = auctionId;
        FileName = fileName;
        ContentType = contentType;
        Content = content;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
    }

    private AuctionImage() { }

    public Guid Id { get; private set; }
    public Guid AuctionId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/octet-stream";
    public byte[] Content { get; private set; } = Array.Empty<byte>();
    public int SortOrder { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static AuctionImage Create(
        Guid auctionId,
        string fileName,
        string contentType,
        byte[] content,
        int sortOrder = 0,
        DateTime? createdAtUtc = null)
    {
        if (auctionId == Guid.Empty)
        {
            throw new ArgumentException("AuctionId is required.", nameof(auctionId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (content is null || content.Length == 0)
        {
            throw new ArgumentException("Image content is required.", nameof(content));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order cannot be negative.");
        }

        var now = EnsureUtcOrDefault(createdAtUtc);
        return new AuctionImage(
            Guid.NewGuid(),
            auctionId,
            fileName.Trim(),
            contentType.Trim(),
            content,
            sortOrder,
            now);
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

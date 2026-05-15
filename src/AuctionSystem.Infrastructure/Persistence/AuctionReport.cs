namespace AuctionSystem.Infrastructure.Persistence;

public sealed class AuctionReport
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid ReportedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Status { get; set; } = OpenStatus;
    public string? ResolutionNote { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public const string OpenStatus = "Open";
    public const string ResolvedStatus = "Resolved";
}
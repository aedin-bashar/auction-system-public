namespace AuctionSystem.Infrastructure.Persistence;

public sealed class AdminTransactionRefund
{
    public Guid TransactionId { get; set; }
    public Guid RefundedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime RefundedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement;

public sealed record AdminTransactionListItemDto(
    Guid TransactionId,
    Guid UserId,
    string UserName,
    string Type,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record AdminTransactionDetailDto(
    Guid TransactionId,
    Guid UserId,
    string UserName,
    string Type,
    decimal Amount,
    string Currency,
    string Status,
    string? Reference,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? RefundedAtUtc,
    string? RefundedBy,
    string? RefundReason,
    decimal? WalletBalanceAmount,
    string? WalletBalanceCurrency);

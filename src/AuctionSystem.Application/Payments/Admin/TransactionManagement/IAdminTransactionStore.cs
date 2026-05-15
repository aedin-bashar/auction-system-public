namespace AuctionSystem.Application.Payments.Admin.TransactionManagement;

public interface IAdminTransactionStore
{
    Task<IReadOnlyList<AdminTransactionListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AdminTransactionDetailDto?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<AdminTransactionDetailDto?> ProcessRefundAsync(ProcessAdminRefundRequest request, CancellationToken cancellationToken = default);
}

public sealed record ProcessAdminRefundRequest(
    Guid TransactionId,
    Guid RefundedByUserId,
    string? Reason,
    DateTime RequestedAtUtc);

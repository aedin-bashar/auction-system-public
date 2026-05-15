using MediatR;

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund;

public sealed record ProcessRefundCommand(
    Guid RequesterUserId,
    Guid TransactionId,
    string? Reason) : IRequest<AdminTransactionDetailDto>;

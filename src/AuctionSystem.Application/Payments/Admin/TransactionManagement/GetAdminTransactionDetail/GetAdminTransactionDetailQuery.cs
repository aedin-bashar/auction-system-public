using MediatR;

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactionDetail;

public sealed record GetAdminTransactionDetailQuery(Guid RequesterUserId, Guid TransactionId) : IRequest<AdminTransactionDetailDto>;

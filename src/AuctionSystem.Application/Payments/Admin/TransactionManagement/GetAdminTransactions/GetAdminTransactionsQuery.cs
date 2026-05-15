using MediatR;

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactions;

public sealed record GetAdminTransactionsQuery(Guid RequesterUserId) : IRequest<IReadOnlyList<AdminTransactionListItemDto>>;

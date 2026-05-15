using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactions;

public sealed class GetAdminTransactionsQueryHandler : IRequestHandler<GetAdminTransactionsQuery, IReadOnlyList<AdminTransactionListItemDto>>
{
    private readonly IUserRepository _users;
    private readonly IAdminTransactionStore _transactions;

    public GetAdminTransactionsQueryHandler(IUserRepository users, IAdminTransactionStore transactions)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    }

    public async Task<IReadOnlyList<AdminTransactionListItemDto>> Handle(GetAdminTransactionsQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        await EnsureRequesterIsActiveAdminAsync(request.RequesterUserId, cancellationToken);

        return await _transactions.ListAsync(cancellationToken);
    }

    private async Task EnsureRequesterIsActiveAdminAsync(Guid requesterUserId, CancellationToken cancellationToken)
    {
        var requester = await _users.GetByIdAsync(requesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can manage transactions.");
        }
    }
}

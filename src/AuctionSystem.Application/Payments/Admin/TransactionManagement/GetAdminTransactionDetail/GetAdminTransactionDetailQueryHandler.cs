using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactionDetail;

public sealed class GetAdminTransactionDetailQueryHandler : IRequestHandler<GetAdminTransactionDetailQuery, AdminTransactionDetailDto>
{
    private readonly IUserRepository _users;
    private readonly IAdminTransactionStore _transactions;

    public GetAdminTransactionDetailQueryHandler(IUserRepository users, IAdminTransactionStore transactions)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    }

    public async Task<AdminTransactionDetailDto> Handle(GetAdminTransactionDetailQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        await EnsureRequesterIsActiveAdminAsync(request.RequesterUserId, cancellationToken);

        var transaction = await _transactions.GetByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null)
        {
            throw new KeyNotFoundException("Transaction was not found.");
        }

        return transaction;
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

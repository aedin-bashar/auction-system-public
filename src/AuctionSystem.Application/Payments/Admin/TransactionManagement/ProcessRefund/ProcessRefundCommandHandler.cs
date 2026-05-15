using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund;

public sealed class ProcessRefundCommandHandler : IRequestHandler<ProcessRefundCommand, AdminTransactionDetailDto>
{
    private readonly IUserRepository _users;
    private readonly IAdminTransactionStore _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessRefundCommandHandler(
        IUserRepository users,
        IAdminTransactionStore transactions,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AdminTransactionDetailDto> Handle(ProcessRefundCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can manage transactions.");
        }

        var refundedTransaction = await _transactions.ProcessRefundAsync(
            new ProcessAdminRefundRequest(
                request.TransactionId,
                request.RequesterUserId,
                string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
                DateTime.UtcNow),
            cancellationToken);

        if (refundedTransaction is null)
        {
            throw new KeyNotFoundException("Transaction was not found.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return refundedTransaction;
    }
}

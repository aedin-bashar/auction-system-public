using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Users.Admin.UserManagement.DeleteUserByAdmin;

public sealed class DeleteUserByAdminCommandHandler : IRequestHandler<DeleteUserByAdminCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserByAdminCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Unit> Handle(DeleteUserByAdminCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can manage users.");
        }

        if (request.RequesterUserId == request.TargetUserId)
        {
            throw new InvalidOperationException("Administrators cannot delete their own account.");
        }

        var targetUser = await _users.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser is null)
        {
            throw new KeyNotFoundException("Target user was not found.");
        }

        _users.Remove(targetUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

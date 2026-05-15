using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Users.Security.ChangePassword;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IPasswordVerifier _passwordVerifier;
    private readonly IPasswordStore _passwordStore;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        IUserRepository users,
        IPasswordVerifier passwordVerifier,
        IPasswordStore passwordStore,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _passwordVerifier = passwordVerifier ?? throw new ArgumentNullException(nameof(passwordVerifier));
        _passwordStore = passwordStore ?? throw new ArgumentNullException(nameof(passwordStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Only active users can change password.");
        }

        var isCurrentPasswordValid = await _passwordVerifier.VerifyAsync(
            request.UserId,
            request.CurrentPassword,
            cancellationToken);
        if (!isCurrentPasswordValid)
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        await _passwordStore.SetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

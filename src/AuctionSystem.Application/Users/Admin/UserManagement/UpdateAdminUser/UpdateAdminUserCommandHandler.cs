using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Users.Admin.UserManagement.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler : IRequestHandler<UpdateAdminUserCommand, AdminUserDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAdminUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AdminUserDto> Handle(UpdateAdminUserCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can manage users.");
        }

        var targetUser = await _users.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser is null)
        {
            throw new KeyNotFoundException("Target user was not found.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(targetUser.Email, normalizedEmail, StringComparison.Ordinal))
        {
            var existingByEmail = await _users.GetByEmailAsync(request.Email, cancellationToken);
            if (existingByEmail is not null && existingByEmail.Id != targetUser.Id)
            {
                throw new InvalidOperationException("Email is already in use.");
            }

            targetUser.ChangeEmail(request.Email);
        }

        targetUser.UpdateProfile(request.FullName, request.PhoneNumber);

        var parsedRole = ParseRole(request.Role);
        if (targetUser.Role != parsedRole)
        {
            targetUser.ChangeRole(parsedRole);
        }

        if (request.IsActive && !targetUser.IsActive)
        {
            targetUser.Activate();
        }
        else if (!request.IsActive && targetUser.IsActive)
        {
            if (targetUser.Id == requester.Id)
            {
                throw new InvalidOperationException("Administrators cannot deactivate their own account.");
            }

            targetUser.Deactivate();
        }

        _users.Update(targetUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminUserDto(
            targetUser.Id,
            targetUser.Email,
            targetUser.FullName,
            targetUser.PhoneNumber,
            targetUser.Role.ToString(),
            targetUser.IsActive,
            targetUser.CreatedAtUtc,
            targetUser.UpdatedAtUtc);
    }

    private static UserRole ParseRole(string role)
    {
        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole) ||
            !Enum.IsDefined(typeof(UserRole), parsedRole))
        {
            throw new ArgumentOutOfRangeException(nameof(role), "Invalid role.");
        }

        return parsedRole;
    }
}

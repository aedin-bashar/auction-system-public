using MediatR;

namespace AuctionSystem.Application.Users.Admin.UserManagement.UpdateAdminUser;

public sealed record UpdateAdminUserCommand(
    Guid RequesterUserId,
    Guid TargetUserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Role,
    bool IsActive) : IRequest<AdminUserDto>;

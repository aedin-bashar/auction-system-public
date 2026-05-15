using MediatR;

namespace AuctionSystem.Application.Users.Admin.UserManagement.DeleteUserByAdmin;

public sealed record DeleteUserByAdminCommand(
    Guid RequesterUserId,
    Guid TargetUserId) : IRequest<Unit>;

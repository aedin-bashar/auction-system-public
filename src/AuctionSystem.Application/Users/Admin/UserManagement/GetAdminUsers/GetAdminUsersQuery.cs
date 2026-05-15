using MediatR;

namespace AuctionSystem.Application.Users.Admin.UserManagement.GetAdminUsers;

public sealed record GetAdminUsersQuery(Guid RequesterUserId) : IRequest<IReadOnlyList<AdminUserDto>>;

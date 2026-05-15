using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Users.Admin.UserManagement.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, IReadOnlyList<AdminUserDto>>
{
    private readonly IUserRepository _users;

    public GetAdminUsersQueryHandler(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<IReadOnlyList<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can manage users.");
        }

        var users = await _users.ListAsync(_ => true, cancellationToken);

        return users
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminUserDto(
                x.Id,
                x.Email,
                x.FullName,
                x.PhoneNumber,
                x.Role.ToString(),
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToList();
    }
}

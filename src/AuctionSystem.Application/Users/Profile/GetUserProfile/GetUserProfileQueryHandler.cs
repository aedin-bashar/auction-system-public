using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Users.Profile.GetUserProfile;

public sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserRepository _users;

    public GetUserProfileQueryHandler(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User was not found.");
        }

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }
}

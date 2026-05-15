using MediatR;

namespace AuctionSystem.Application.Users.Profile.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber) : IRequest<UserProfileDto>;

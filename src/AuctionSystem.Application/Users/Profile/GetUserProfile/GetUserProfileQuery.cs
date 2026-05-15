using MediatR;

namespace AuctionSystem.Application.Users.Profile.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;

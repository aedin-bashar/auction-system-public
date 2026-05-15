using FluentValidation;

namespace AuctionSystem.Application.Users.Profile.GetUserProfile;

public sealed class GetUserProfileQueryValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}

using FluentValidation;

namespace AuctionSystem.Application.Users.Admin.UserManagement.GetAdminUsers;

public sealed class GetAdminUsersQueryValidator : AbstractValidator<GetAdminUsersQuery>
{
    public GetAdminUsersQueryValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();
    }
}

using AuctionSystem.Domain.Users;
using FluentValidation;

namespace AuctionSystem.Application.Users.Admin.UserManagement.UpdateAdminUser;

public sealed class UpdateAdminUserCommandValidator : AbstractValidator<UpdateAdminUserCommand>
{
    public UpdateAdminUserCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.TargetUserId)
            .NotEmpty();

        RuleFor(x => x.TargetUserId)
            .NotEqual(x => x.RequesterUserId);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(32);

        RuleFor(x => x.Role)
            .NotEmpty()
            .MaximumLength(32)
            .Must(BeValidRole)
            .WithMessage("Role must be one of: Admin, Seller, Bidder.");
    }

    private static bool BeValidRole(string role)
    {
        return Enum.TryParse<UserRole>(role, true, out var parsedRole) &&
               Enum.IsDefined(typeof(UserRole), parsedRole);
    }
}

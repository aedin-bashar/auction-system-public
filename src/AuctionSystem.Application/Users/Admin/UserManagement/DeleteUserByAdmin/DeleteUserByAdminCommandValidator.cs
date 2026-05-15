using FluentValidation;

namespace AuctionSystem.Application.Users.Admin.UserManagement.DeleteUserByAdmin;

public sealed class DeleteUserByAdminCommandValidator : AbstractValidator<DeleteUserByAdminCommand>
{
    public DeleteUserByAdminCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.TargetUserId)
            .NotEmpty();

        RuleFor(x => x.TargetUserId)
            .NotEqual(x => x.RequesterUserId)
            .WithMessage("Administrators cannot delete their own account.");
    }
}

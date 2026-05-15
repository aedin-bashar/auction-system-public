using FluentValidation;

namespace AuctionSystem.Application.Admin.SystemSettings.UpsertAdminSystemSetting;

public sealed class UpsertAdminSystemSettingCommandValidator : AbstractValidator<UpsertAdminSystemSettingCommand>
{
    public UpsertAdminSystemSettingCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-zA-Z0-9._:-]+$")
            .WithMessage("Setting key may contain only letters, numbers, dot, underscore, colon, or hyphen.");

        RuleFor(x => x.Value)
            .NotNull()
            .MaximumLength(2000);
    }
}

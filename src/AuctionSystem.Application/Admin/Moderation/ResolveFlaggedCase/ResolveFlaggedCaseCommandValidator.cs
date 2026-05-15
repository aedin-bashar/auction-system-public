using FluentValidation;

namespace AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase;

public sealed class ResolveFlaggedCaseCommandValidator : AbstractValidator<ResolveFlaggedCaseCommand>
{
    public ResolveFlaggedCaseCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.CaseId)
            .NotEmpty();

        RuleFor(x => x.ResolutionNote)
            .Must(note => note is null || !string.IsNullOrWhiteSpace(note))
            .WithMessage("Resolution note cannot be empty.")
            .When(x => x.ResolutionNote is not null)
            .MaximumLength(1000);
    }
}
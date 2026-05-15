using FluentValidation;

namespace AuctionSystem.Application.Auctions.ReportAuction;

public sealed class ReportAuctionCommandValidator : AbstractValidator<ReportAuctionCommand>
{
    public ReportAuctionCommandValidator()
    {
        RuleFor(x => x.AuctionId)
            .NotEmpty();

        RuleFor(x => x.ReportedByUserId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(80);

        RuleFor(x => x.Details)
            .Must(details => details is null || !string.IsNullOrWhiteSpace(details))
            .WithMessage("Details cannot be empty.")
            .When(x => x.Details is not null)
            .MaximumLength(1000);
    }
}
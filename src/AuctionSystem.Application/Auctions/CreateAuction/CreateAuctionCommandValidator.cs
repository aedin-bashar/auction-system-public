using FluentValidation;
using System.Text.RegularExpressions;

namespace AuctionSystem.Application.Auctions.CreateAuction;

public sealed class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    private static readonly Regex CurrencyCodeRegex = new("^[A-Za-z]{3}$", RegexOptions.Compiled);
    private readonly Func<DateTime> _utcNowProvider;

    public CreateAuctionCommandValidator(Func<DateTime>? utcNowProvider = null)
    {
        _utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);

        RuleFor(x => x.SellerId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(120);

        RuleFor(x => x.Category)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .Must(description => description is null || !string.IsNullOrWhiteSpace(description))
            .WithMessage("Description cannot be empty.")
            .When(x => x.Description is not null)
            .MaximumLength(2000);

        RuleFor(x => x.StartingPriceAmount)
            .GreaterThanOrEqualTo(0m);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches(CurrencyCodeRegex);

        RuleFor(x => x.EndTimeUtc)
            .Must(endTime => endTime > _utcNowProvider())
            .WithMessage("EndTimeUtc must be in the future.");
    }
}
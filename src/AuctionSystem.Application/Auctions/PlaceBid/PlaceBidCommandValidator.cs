using FluentValidation;
using System.Text.RegularExpressions;

namespace AuctionSystem.Application.Auctions.PlaceBid;

public sealed class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    private static readonly Regex CurrencyCodeRegex = new("^[A-Za-z]{3}$", RegexOptions.Compiled);

    public PlaceBidCommandValidator()
    {
        RuleFor(x => x.AuctionId)
            .NotEmpty();

        RuleFor(x => x.BidderId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0m);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches(CurrencyCodeRegex);
    }
}
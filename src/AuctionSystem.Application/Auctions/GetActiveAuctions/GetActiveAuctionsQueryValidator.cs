using FluentValidation;

namespace AuctionSystem.Application.Auctions.GetActiveAuctions;

public sealed class GetActiveAuctionsQueryValidator : AbstractValidator<GetActiveAuctionsQuery>
{
    public GetActiveAuctionsQueryValidator()
    {
        RuleFor(x => x.Category)
            .Must(category => category is null || !string.IsNullOrWhiteSpace(category))
            .WithMessage("Category cannot be empty.")
            .When(x => x.Category is not null)
            .MaximumLength(100);

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice.Value <= x.MaxPrice.Value)
            .WithMessage("MinPrice must be less than or equal to MaxPrice.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 1000);
    }
}
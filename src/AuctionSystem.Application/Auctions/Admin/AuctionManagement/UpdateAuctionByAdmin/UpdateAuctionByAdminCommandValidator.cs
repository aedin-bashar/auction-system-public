using FluentValidation;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.UpdateAuctionByAdmin;

public sealed class UpdateAuctionByAdminCommandValidator : AbstractValidator<UpdateAuctionByAdminCommand>
{
    public UpdateAuctionByAdminCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.AuctionId)
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
            .MaximumLength(2000);

        RuleFor(x => x.StartingPriceAmount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.EndTimeUtc)
            .NotEmpty();
    }
}

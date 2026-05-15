using FluentValidation;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.StartAuctionByAdmin;

public sealed class StartAuctionByAdminCommandValidator : AbstractValidator<StartAuctionByAdminCommand>
{
    public StartAuctionByAdminCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.AuctionId)
            .NotEmpty();
    }
}

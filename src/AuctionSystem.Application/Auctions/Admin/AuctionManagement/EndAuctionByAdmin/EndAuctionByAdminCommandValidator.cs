using FluentValidation;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin;

public sealed class EndAuctionByAdminCommandValidator : AbstractValidator<EndAuctionByAdminCommand>
{
    public EndAuctionByAdminCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.AuctionId)
            .NotEmpty();
    }
}
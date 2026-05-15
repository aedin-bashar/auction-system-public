using FluentValidation;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.DeleteAuctionByAdmin;

public sealed class DeleteAuctionByAdminCommandValidator : AbstractValidator<DeleteAuctionByAdminCommand>
{
    public DeleteAuctionByAdminCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.AuctionId)
            .NotEmpty();
    }
}
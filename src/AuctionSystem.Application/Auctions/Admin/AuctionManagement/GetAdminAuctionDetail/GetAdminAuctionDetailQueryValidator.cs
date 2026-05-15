using FluentValidation;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctionDetail;

public sealed class GetAdminAuctionDetailQueryValidator : AbstractValidator<GetAdminAuctionDetailQuery>
{
    public GetAdminAuctionDetailQueryValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.AuctionId)
            .NotEmpty();
    }
}

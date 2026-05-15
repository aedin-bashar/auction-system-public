using FluentValidation;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctions;

public sealed class GetAdminAuctionsQueryValidator : AbstractValidator<GetAdminAuctionsQuery>
{
    public GetAdminAuctionsQueryValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();
    }
}

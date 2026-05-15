using FluentValidation;

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactionDetail;

public sealed class GetAdminTransactionDetailQueryValidator : AbstractValidator<GetAdminTransactionDetailQuery>
{
    public GetAdminTransactionDetailQueryValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.TransactionId)
            .NotEmpty();
    }
}

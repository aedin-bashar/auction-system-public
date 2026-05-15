using FluentValidation;

namespace AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactions;

public sealed class GetAdminTransactionsQueryValidator : AbstractValidator<GetAdminTransactionsQuery>
{
    public GetAdminTransactionsQueryValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();
    }
}

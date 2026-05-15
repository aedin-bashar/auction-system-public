using FluentValidation;

namespace AuctionSystem.Application.Users.PaymentMethods.GetPaymentMethods;

public sealed class GetPaymentMethodsQueryValidator : AbstractValidator<GetPaymentMethodsQuery>
{
    public GetPaymentMethodsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}

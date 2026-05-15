using FluentValidation;

namespace AuctionSystem.Application.Users.PaymentMethods.RemovePaymentMethod;

public sealed class RemovePaymentMethodCommandValidator : AbstractValidator<RemovePaymentMethodCommand>
{
    public RemovePaymentMethodCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty();
    }
}

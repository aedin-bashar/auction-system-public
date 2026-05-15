using FluentValidation;

namespace AuctionSystem.Application.Users.PaymentMethods.AddPaymentMethod;

public sealed class AddPaymentMethodCommandValidator : AbstractValidator<AddPaymentMethodCommand>
{
    public AddPaymentMethodCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Type)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Last4)
            .NotEmpty()
            .Length(4)
            .Matches("^[0-9]{4}$");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12);

        RuleFor(x => x.ExpiryYear)
            .InclusiveBetween(DateTime.UtcNow.Year, DateTime.UtcNow.Year + 30);

        RuleFor(x => x.HolderName)
            .MaximumLength(120);
    }
}

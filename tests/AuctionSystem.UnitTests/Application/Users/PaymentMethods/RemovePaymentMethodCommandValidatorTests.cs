using AuctionSystem.Application.Users.PaymentMethods.RemovePaymentMethod;

namespace AuctionSystem.UnitTests.Application.Users.PaymentMethods;

public class RemovePaymentMethodCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new RemovePaymentMethodCommandValidator();
        var command = new RemovePaymentMethodCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsEmpty_Fails()
    {
        var validator = new RemovePaymentMethodCommandValidator();
        var command = new RemovePaymentMethodCommand(Guid.Empty, Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
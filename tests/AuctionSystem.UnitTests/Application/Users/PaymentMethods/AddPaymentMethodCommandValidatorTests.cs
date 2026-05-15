using AuctionSystem.Application.Users.PaymentMethods.AddPaymentMethod;

namespace AuctionSystem.UnitTests.Application.Users.PaymentMethods;

public class AddPaymentMethodCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new AddPaymentMethodCommandValidator();
        var command = new AddPaymentMethodCommand(Guid.NewGuid(), "Card", "Visa", "4242", 12, DateTime.UtcNow.Year + 1, "Payment User", true);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "Card", "Visa", "4242", 12, 2030)]
    [InlineData("00000000-0000-0000-0000-000000000001", "", "Visa", "4242", 12, 2030)]
    [InlineData("00000000-0000-0000-0000-000000000001", "Card", "", "4242", 12, 2030)]
    [InlineData("00000000-0000-0000-0000-000000000001", "Card", "Visa", "42", 12, 2030)]
    [InlineData("00000000-0000-0000-0000-000000000001", "Card", "Visa", "ABCD", 12, 2030)]
    [InlineData("00000000-0000-0000-0000-000000000001", "Card", "Visa", "4242", 0, 2030)]
    [InlineData("00000000-0000-0000-0000-000000000001", "Card", "Visa", "4242", 13, 2030)]
    public void Validate_WithInvalidBasicFields_Fails(string userIdValue, string type, string provider, string last4, int expiryMonth, int expiryYear)
    {
        var validator = new AddPaymentMethodCommandValidator();
        var userId = Guid.Parse(userIdValue);
        var command = new AddPaymentMethodCommand(userId, type, provider, last4, expiryMonth, expiryYear, "Payment User", true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenExpiryYearTooOld_Fails()
    {
        var validator = new AddPaymentMethodCommandValidator();
        var command = new AddPaymentMethodCommand(Guid.NewGuid(), "Card", "Visa", "4242", 12, DateTime.UtcNow.Year - 1, "Payment User", true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenHolderNameTooLong_Fails()
    {
        var validator = new AddPaymentMethodCommandValidator();
        var command = new AddPaymentMethodCommand(Guid.NewGuid(), "Card", "Visa", "4242", 12, DateTime.UtcNow.Year + 1, new string('x', 121), true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
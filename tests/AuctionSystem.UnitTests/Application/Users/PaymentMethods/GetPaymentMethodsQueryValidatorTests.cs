using AuctionSystem.Application.Users.PaymentMethods.GetPaymentMethods;

namespace AuctionSystem.UnitTests.Application.Users.PaymentMethods;

public class GetPaymentMethodsQueryValidatorTests
{
    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var validator = new GetPaymentMethodsQueryValidator();
        var query = new GetPaymentMethodsQuery(Guid.NewGuid());

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUserIdEmpty_Fails()
    {
        var validator = new GetPaymentMethodsQueryValidator();
        var query = new GetPaymentMethodsQuery(Guid.Empty);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
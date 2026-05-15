using AuctionSystem.Application.Auctions.PlaceBid;

namespace AuctionSystem.UnitTests.Application.Auctions.PlaceBid;

public class PlaceBidCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new PlaceBidCommandValidator();
        var command = new PlaceBidCommand(Guid.NewGuid(), Guid.NewGuid(), 150m, "USD");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsEmpty_Fails()
    {
        var validator = new PlaceBidCommandValidator();
        var command = new PlaceBidCommand(Guid.Empty, Guid.Empty, 150m, "USD");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenAmountNotPositive_Fails(decimal amount)
    {
        var validator = new PlaceBidCommandValidator();
        var command = new PlaceBidCommand(Guid.NewGuid(), Guid.NewGuid(), amount, "USD");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("12$")]
    public void Validate_WhenCurrencyInvalid_Fails(string currency)
    {
        var validator = new PlaceBidCommandValidator();
        var command = new PlaceBidCommand(Guid.NewGuid(), Guid.NewGuid(), 150m, currency);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
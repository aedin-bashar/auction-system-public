using AuctionSystem.Application.Auctions.Admin.AuctionManagement.UpdateAuctionByAdmin;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement;

public class UpdateAuctionByAdminCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new UpdateAuctionByAdminCommandValidator();
        var command = new UpdateAuctionByAdminCommand(Guid.NewGuid(), Guid.NewGuid(), "Title", "Art", "Description", 100m, "USD", DateTime.UtcNow.AddDays(5));

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Art", 100, "USD")]
    [InlineData("AB", "Art", 100, "USD")]
    [InlineData("Title", "", 100, "USD")]
    [InlineData("Title", "A", 100, "USD")]
    [InlineData("Title", "Art", -1, "USD")]
    [InlineData("Title", "Art", 100, "")]
    [InlineData("Title", "Art", 100, "US")]
    public void Validate_WithInvalidFields_Fails(string title, string category, decimal price, string currency)
    {
        var validator = new UpdateAuctionByAdminCommandValidator();
        var command = new UpdateAuctionByAdminCommand(Guid.NewGuid(), Guid.NewGuid(), title, category, "Description", price, currency, DateTime.UtcNow.AddDays(5));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsEmpty_Fails()
    {
        var validator = new UpdateAuctionByAdminCommandValidator();
        var command = new UpdateAuctionByAdminCommand(Guid.Empty, Guid.Empty, "Title", "Art", "Description", 100m, "USD", DateTime.UtcNow.AddDays(5));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
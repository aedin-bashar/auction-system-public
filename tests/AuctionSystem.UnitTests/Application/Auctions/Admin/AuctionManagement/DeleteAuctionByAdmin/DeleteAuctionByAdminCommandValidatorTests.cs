using AuctionSystem.Application.Auctions.Admin.AuctionManagement.DeleteAuctionByAdmin;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement;

public class DeleteAuctionByAdminCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new DeleteAuctionByAdminCommandValidator();
        var command = new DeleteAuctionByAdminCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_Fails()
    {
        var validator = new DeleteAuctionByAdminCommandValidator();
        var command = new DeleteAuctionByAdminCommand(Guid.Empty, Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
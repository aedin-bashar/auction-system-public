namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin;

public class EndAuctionByAdminCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommandValidator();
        var command = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_Fails()
    {
        var validator = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommandValidator();
        var command = new AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin.EndAuctionByAdminCommand(Guid.Empty, Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
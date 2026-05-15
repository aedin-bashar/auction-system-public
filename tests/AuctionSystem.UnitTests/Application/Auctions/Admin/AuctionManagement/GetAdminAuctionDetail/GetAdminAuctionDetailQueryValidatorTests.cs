using AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctionDetail;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement;

public class GetAdminAuctionDetailQueryValidatorTests
{
    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var validator = new GetAdminAuctionDetailQueryValidator();
        var query = new GetAdminAuctionDetailQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsEmpty_Fails()
    {
        var validator = new GetAdminAuctionDetailQueryValidator();
        var query = new GetAdminAuctionDetailQuery(Guid.Empty, Guid.Empty);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
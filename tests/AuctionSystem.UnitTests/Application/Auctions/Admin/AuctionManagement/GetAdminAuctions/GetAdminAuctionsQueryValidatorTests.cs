using AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctions;

namespace AuctionSystem.UnitTests.Application.Auctions.Admin.AuctionManagement;

public class GetAdminAuctionsQueryValidatorTests
{
    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var validator = new GetAdminAuctionsQueryValidator();
        var query = new GetAdminAuctionsQuery(Guid.NewGuid());

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRequesterIdEmpty_Fails()
    {
        var validator = new GetAdminAuctionsQueryValidator();
        var query = new GetAdminAuctionsQuery(Guid.Empty);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
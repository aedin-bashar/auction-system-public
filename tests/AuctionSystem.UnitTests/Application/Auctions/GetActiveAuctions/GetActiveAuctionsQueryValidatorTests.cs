using AuctionSystem.Application.Auctions.GetActiveAuctions;

namespace AuctionSystem.UnitTests.Application.Auctions.GetActiveAuctions;

public class GetActiveAuctionsQueryValidatorTests
{
    [Fact]
    public void Validate_WithValidFilters_Passes()
    {
        var validator = new GetActiveAuctionsQueryValidator();
        var query = new GetActiveAuctionsQuery(
            Category: "electronics",
            MinPrice: 100m,
            MaxPrice: 1000m,
            PageNumber: 1,
            PageSize: 20);

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenMinPriceGreaterThanMaxPrice_Fails()
    {
        var validator = new GetActiveAuctionsQueryValidator();
        var query = new GetActiveAuctionsQuery(
            Category: "electronics",
            MinPrice: 1000m,
            MaxPrice: 100m,
            PageNumber: 1,
            PageSize: 20);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
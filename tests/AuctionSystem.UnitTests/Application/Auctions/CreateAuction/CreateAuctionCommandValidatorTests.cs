using AuctionSystem.Application.Auctions.CreateAuction;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.CreateAuction;

public class CreateAuctionCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var now = new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);
        var utcNowProvider = new Mock<Func<DateTime>>();
        utcNowProvider.Setup(provider => provider()).Returns(now);

        var validator = new CreateAuctionCommandValidator(utcNowProvider.Object);
        var command = new CreateAuctionCommand(
            SellerId: Guid.NewGuid(),
            Title: "Gaming Laptop",
            Category: "Electronics",
            Description: "High-end laptop in excellent condition",
            StartingPriceAmount: 500m,
            Currency: "USD",
            EndTimeUtc: now.AddHours(2));

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Electronics", "Valid description", 500, "USD", 2)]
    [InlineData("AB", "Electronics", "Valid description", 500, "USD", 2)]
    [InlineData("Valid title", "", "Valid description", 500, "USD", 2)]
    [InlineData("Valid title", "A", "Valid description", 500, "USD", 2)]
    [InlineData("Valid title", "Electronics", "   ", 500, "USD", 2)]
    [InlineData("Valid title", "Electronics", "Valid description", -1, "USD", 2)]
    [InlineData("Valid title", "Electronics", "Valid description", 500, "US", 2)]
    [InlineData("Valid title", "Electronics", "Valid description", 500, "12$", 2)]
    [InlineData("Valid title", "Electronics", "Valid description", 500, "USD", 0)]
    [InlineData("Valid title", "Electronics", "Valid description", 500, "USD", -1)]
    public void Validate_WithInvalidCommand_Fails(
        string title,
        string category,
        string? description,
        decimal startingPriceAmount,
        string currency,
        int endHoursOffset)
    {
        var now = new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);
        var utcNowProvider = new Mock<Func<DateTime>>();
        utcNowProvider.Setup(provider => provider()).Returns(now);

        var validator = new CreateAuctionCommandValidator(utcNowProvider.Object);
        var command = new CreateAuctionCommand(
            SellerId: Guid.NewGuid(),
            Title: title,
            Category: category,
            Description: description,
            StartingPriceAmount: startingPriceAmount,
            Currency: currency,
            EndTimeUtc: now.AddHours(endHoursOffset));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
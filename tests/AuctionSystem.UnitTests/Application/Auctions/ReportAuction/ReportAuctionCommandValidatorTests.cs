namespace AuctionSystem.UnitTests.Application.Auctions.ReportAuction;

public class ReportAuctionCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandValidator();
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fraudulent listing",
            "Suspicious description");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_Fails()
    {
        var validator = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandValidator();
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            Guid.Empty,
            Guid.Empty,
            "Fraudulent listing",
            null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    public void Validate_WhenReasonIsTooShortOrEmpty_Fails(string reason)
    {
        var validator = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandValidator();
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            reason,
            null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenDetailsAreWhitespace_Fails()
    {
        var validator = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandValidator();
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fraudulent listing",
            "   ");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenDetailsAreTooLong_Fails()
    {
        var validator = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommandValidator();
        var command = new AuctionSystem.Application.Auctions.ReportAuction.ReportAuctionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fraudulent listing",
            new string('x', 1001));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
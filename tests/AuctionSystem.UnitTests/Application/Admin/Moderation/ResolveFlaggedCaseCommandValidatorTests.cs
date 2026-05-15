namespace AuctionSystem.UnitTests.Application.Admin.Moderation;

public class ResolveFlaggedCaseCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommandValidator();
        var command = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Reviewed and resolved");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_Fails()
    {
        var validator = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommandValidator();
        var command = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommand(
            Guid.Empty,
            Guid.Empty,
            null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenResolutionNoteIsWhitespace_Fails()
    {
        var validator = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommandValidator();
        var command = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "   ");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenResolutionNoteTooLong_Fails()
    {
        var validator = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommandValidator();
        var command = new AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase.ResolveFlaggedCaseCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('x', 1001));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
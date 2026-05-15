using AuctionSystem.Application.Admin.Reports.GenerateAdminReport;

namespace AuctionSystem.UnitTests.Application.Admin.Reports.GenerateAdminReport;

public class GenerateAdminReportCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new GenerateAdminReportCommandValidator();
        var command = new GenerateAdminReportCommand(Guid.NewGuid(), "Revenue", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRequesterIdEmpty_Fails()
    {
        var validator = new GenerateAdminReportCommandValidator();
        var command = new GenerateAdminReportCommand(Guid.Empty, "Revenue", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenReportTypeEmpty_Fails()
    {
        var validator = new GenerateAdminReportCommandValidator();
        var command = new GenerateAdminReportCommand(Guid.NewGuid(), string.Empty, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRangeStartAfterEnd_Fails()
    {
        var validator = new GenerateAdminReportCommandValidator();
        var command = new GenerateAdminReportCommand(Guid.NewGuid(), "Revenue", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRangeExceeds366Days_Fails()
    {
        var validator = new GenerateAdminReportCommandValidator();
        var command = new GenerateAdminReportCommand(Guid.NewGuid(), "Revenue", DateTime.UtcNow.AddDays(-367), DateTime.UtcNow);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
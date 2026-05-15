using AuctionSystem.Application.Admin.SystemSettings.UpsertAdminSystemSetting;

namespace AuctionSystem.UnitTests.Application.Admin.SystemSettings.UpsertAdminSystemSetting;

public class UpsertAdminSystemSettingCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new UpsertAdminSystemSettingCommandValidator();
        var command = new UpsertAdminSystemSettingCommand(Guid.NewGuid(), "payments.demo-enabled", "true");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRequesterIdEmpty_Fails()
    {
        var validator = new UpsertAdminSystemSettingCommandValidator();
        var command = new UpsertAdminSystemSettingCommand(Guid.Empty, "payments.demo-enabled", "true");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid key!")]
    public void Validate_WhenKeyInvalid_Fails(string key)
    {
        var validator = new UpsertAdminSystemSettingCommandValidator();
        var command = new UpsertAdminSystemSettingCommand(Guid.NewGuid(), key, "true");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenValueIsNull_Fails()
    {
        var validator = new UpsertAdminSystemSettingCommandValidator();
        var command = new UpsertAdminSystemSettingCommand(Guid.NewGuid(), "payments.demo-enabled", null!);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenValueTooLong_Fails()
    {
        var validator = new UpsertAdminSystemSettingCommandValidator();
        var command = new UpsertAdminSystemSettingCommand(Guid.NewGuid(), "payments.demo-enabled", new string('x', 2001));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
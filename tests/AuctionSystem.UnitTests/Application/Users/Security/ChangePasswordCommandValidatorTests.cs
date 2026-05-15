using AuctionSystem.Application.Users.Security.ChangePassword;

namespace AuctionSystem.UnitTests.Application.Users.Security;

public class ChangePasswordCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.NewGuid(), "Current123!", "NewSecret123!");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUserIdEmpty_Fails()
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.Empty, "Current123!", "NewSecret123!");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("", "NewSecret123!")]
    [InlineData("short", "NewSecret123!")]
    [InlineData("Current123!", "")]
    [InlineData("Current123!", "short")]
    public void Validate_WhenPasswordsTooShortOrEmpty_Fails(string currentPassword, string newPassword)
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.NewGuid(), currentPassword, newPassword);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNewPasswordMatchesCurrentPassword_Fails()
    {
        var validator = new ChangePasswordCommandValidator();
        var command = new ChangePasswordCommand(Guid.NewGuid(), "SamePassword123!", "SamePassword123!");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
using AuctionSystem.Application.Users.Profile.UpdateUserProfile;

namespace AuctionSystem.UnitTests.Application.Users.Profile;

public class UpdateUserProfileCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new UpdateUserProfileCommandValidator();
        var command = new UpdateUserProfileCommand(Guid.NewGuid(), "user@example.com", "Valid User", "+1 555 000 0000");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Valid User", "+1 555 000 0000")]
    [InlineData("not-an-email", "Valid User", "+1 555 000 0000")]
    [InlineData("user@example.com", "", "+1 555 000 0000")]
    [InlineData("user@example.com", "A", "+1 555 000 0000")]
    public void Validate_WithInvalidEmailOrName_Fails(string email, string fullName, string? phoneNumber)
    {
        var validator = new UpdateUserProfileCommandValidator();
        var command = new UpdateUserProfileCommand(Guid.NewGuid(), email, fullName, phoneNumber);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUserIdEmpty_Fails()
    {
        var validator = new UpdateUserProfileCommandValidator();
        var command = new UpdateUserProfileCommand(Guid.Empty, "user@example.com", "Valid User", "+1 555 000 0000");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPhoneTooLong_Fails()
    {
        var validator = new UpdateUserProfileCommandValidator();
        var command = new UpdateUserProfileCommand(Guid.NewGuid(), "user@example.com", "Valid User", new string('1', 33));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
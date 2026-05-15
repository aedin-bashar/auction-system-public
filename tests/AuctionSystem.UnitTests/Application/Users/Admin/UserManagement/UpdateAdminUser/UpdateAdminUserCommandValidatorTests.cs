using AuctionSystem.Application.Users.Admin.UserManagement.UpdateAdminUser;

namespace AuctionSystem.UnitTests.Application.Users.Admin.UserManagement;

public class UpdateAdminUserCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new UpdateAdminUserCommandValidator();
        var command = new UpdateAdminUserCommand(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", "Valid User", "+1 555 000 0000", "Admin", true);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRequesterEqualsTarget_Fails()
    {
        var validator = new UpdateAdminUserCommandValidator();
        var userId = Guid.NewGuid();
        var command = new UpdateAdminUserCommand(userId, userId, "user@example.com", "Valid User", "+1 555 000 0000", "Admin", true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("", "Valid User", "Admin")]
    [InlineData("not-an-email", "Valid User", "Admin")]
    [InlineData("user@example.com", "", "Admin")]
    [InlineData("user@example.com", "A", "Admin")]
    [InlineData("user@example.com", "Valid User", "UnknownRole")]
    public void Validate_WithInvalidEmailNameOrRole_Fails(string email, string fullName, string role)
    {
        var validator = new UpdateAdminUserCommandValidator();
        var command = new UpdateAdminUserCommand(Guid.NewGuid(), Guid.NewGuid(), email, fullName, "+1 555 000 0000", role, true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPhoneTooLong_Fails()
    {
        var validator = new UpdateAdminUserCommandValidator();
        var command = new UpdateAdminUserCommand(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", "Valid User", new string('1', 33), "Admin", true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
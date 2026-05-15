using AuctionSystem.Application.Users.Admin.UserManagement.DeleteUserByAdmin;

namespace AuctionSystem.UnitTests.Application.Users.Admin.UserManagement;

public class DeleteUserByAdminCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new DeleteUserByAdminCommandValidator();
        var command = new DeleteUserByAdminCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_Fails()
    {
        var validator = new DeleteUserByAdminCommandValidator();
        var command = new DeleteUserByAdminCommand(Guid.Empty, Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRequesterEqualsTarget_Fails()
    {
        var validator = new DeleteUserByAdminCommandValidator();
        var userId = Guid.NewGuid();
        var command = new DeleteUserByAdminCommand(userId, userId);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
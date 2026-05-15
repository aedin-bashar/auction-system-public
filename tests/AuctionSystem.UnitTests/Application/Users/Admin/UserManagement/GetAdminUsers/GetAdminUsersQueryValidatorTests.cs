using AuctionSystem.Application.Users.Admin.UserManagement.GetAdminUsers;

namespace AuctionSystem.UnitTests.Application.Users.Admin.UserManagement;

public class GetAdminUsersQueryValidatorTests
{
    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var validator = new GetAdminUsersQueryValidator();
        var query = new GetAdminUsersQuery(Guid.NewGuid());

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRequesterUserIdEmpty_Fails()
    {
        var validator = new GetAdminUsersQueryValidator();
        var query = new GetAdminUsersQuery(Guid.Empty);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
using AuctionSystem.Application.Users.Profile.GetUserProfile;

namespace AuctionSystem.UnitTests.Application.Users.Profile;

public class GetUserProfileQueryValidatorTests
{
    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var validator = new GetUserProfileQueryValidator();
        var query = new GetUserProfileQuery(Guid.NewGuid());

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUserIdEmpty_Fails()
    {
        var validator = new GetUserProfileQueryValidator();
        var query = new GetUserProfileQuery(Guid.Empty);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuctionSystem.API.Extensions;

namespace AuctionSystem.UnitTests.API.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetRequiredUserId_WhenSubClaimExists_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));

        var result = principal.GetRequiredUserId();

        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetRequiredUserId_WhenSubClaimMissing_FallsBackToNameIdentifier()
    {
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var result = principal.GetRequiredUserId();

        Assert.Equal(userId, result);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("")]
    public void GetRequiredUserId_WhenClaimInvalid_ThrowsUnauthorizedAccessException(string claimValue)
    {
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Sub, claimValue));

        Assert.Throws<UnauthorizedAccessException>(() => principal.GetRequiredUserId());
    }

    [Fact]
    public void GetRequiredUserId_WhenClaimMissing_ThrowsUnauthorizedAccessException()
    {
        var principal = CreatePrincipal();

        Assert.Throws<UnauthorizedAccessException>(() => principal.GetRequiredUserId());
    }

    [Fact]
    public void GetRequiredUserId_WhenPrincipalNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ClaimsPrincipalExtensions.GetRequiredUserId(null!));
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
    }
}
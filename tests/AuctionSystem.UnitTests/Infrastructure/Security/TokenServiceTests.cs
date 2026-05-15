using System.IdentityModel.Tokens.Jwt;
using AuctionSystem.Application.Authentication.Models;
using AuctionSystem.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace AuctionSystem.UnitTests.Infrastructure.Security;

public class TokenServiceTests
{
    [Fact]
    public async Task CreateAccessTokenAsync_WithValidRequest_ReturnsJwtContainingExpectedClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "AuctionSystem.Tests",
            Audience = "AuctionSystem.Tests.Client",
            SigningKey = "ThisIsATestSigningKeyWithAtLeast32Chars!",
            AccessTokenMinutes = 90
        });
        var service = new TokenService(options);
        var request = new TokenRequest(Guid.NewGuid(), "user@example.com", "Bidder");

        var before = DateTime.UtcNow;
        var result = await service.CreateAccessTokenAsync(request, CancellationToken.None);
        var after = DateTime.UtcNow;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal(request.UserId.ToString(), token.Subject);
        Assert.Equal("user@example.com", token.Claims.Single(x => x.Type == "email").Value);
        Assert.Equal("Bidder", token.Claims.Single(x => x.Type == "role").Value);
        Assert.Equal("AuctionSystem.Tests", token.Issuer);
        Assert.Equal("AuctionSystem.Tests.Client", token.Audiences.Single());
        Assert.InRange(result.ExpiresAtUtc, before.AddMinutes(89), after.AddMinutes(91));
    }

    [Fact]
    public async Task CreateAccessTokenAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var service = new TokenService(Options.Create(new JwtOptions
        {
            SigningKey = "ThisIsATestSigningKeyWithAtLeast32Chars!"
        }));
        var request = new TokenRequest(Guid.NewGuid(), "user@example.com", "Bidder");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.CreateAccessTokenAsync(request, cancellationSource.Token));
    }

    [Fact]
    public async Task CreateAccessTokenAsync_WhenSigningKeyTooShort_ThrowsInvalidOperationException()
    {
        var service = new TokenService(Options.Create(new JwtOptions
        {
            SigningKey = "short-key"
        }));
        var request = new TokenRequest(Guid.NewGuid(), "user@example.com", "Bidder");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAccessTokenAsync(request, CancellationToken.None));
    }
}
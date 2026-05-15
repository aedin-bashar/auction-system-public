using System.Net;
using System.Net.Http.Headers;
using AuctionSystem.IntegrationTests.Auth;

namespace AuctionSystem.IntegrationTests.Infrastructure;

public sealed class JwtAuthenticationPipelineTests
{
    [Fact]
    public async Task AuthorizedEndpoint_WithExpiredJwt_ReturnsUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            "expired@example.com",
            "Bidder",
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddMinutes(-1));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auctions/my-bids");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedEndpoint_WithInvalidSignatureJwt_ReturnsUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            "invalid-signature@example.com",
            "Bidder",
            signingKey: "WrongSigningKeyThatStillHasEnoughLength123!");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auctions/my-bids");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedEndpoint_WithMalformedJwt_ReturnsUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt-token");

        var response = await client.GetAsync("/api/auctions/my-bids");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
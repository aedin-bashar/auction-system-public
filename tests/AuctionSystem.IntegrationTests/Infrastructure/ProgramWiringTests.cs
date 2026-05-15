using System.Net;
using AuctionSystem.IntegrationTests.Auth;

namespace AuctionSystem.IntegrationTests.Infrastructure;

public sealed class ProgramWiringTests
{
    [Fact]
    public async Task SwaggerJson_InDevelopment_IsAvailable()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("openapi", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignalRHub_NegotiateEndpoint_IsMapped()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/auctions/negotiate?negotiateVersion=1")
        {
            Content = new StringContent(string.Empty)
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("availableTransports", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorsPolicy_ForConfiguredClientOrigin_AllowsPreflightRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auctions");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }
}
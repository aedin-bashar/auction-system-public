using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Domain.Users;
using AuctionSystem.IntegrationTests.Infrastructure;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionSystem.IntegrationTests.Bidding;

public sealed class ReportAuctionEndpointTests
{
    [Fact]
    public async Task ReportAuction_WithValidRequest_CreatesOpenCase()
    {
        await using var factory = new BiddingWebApplicationFactory();
        var client = factory.CreateClient();

        var (_, bidderId, auctionId) = await factory.SeedActiveAuctionAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.PostAsJsonAsync($"/api/auctions/{auctionId}/reports", new
        {
            reason = "Suspicious listing",
            details = "The title and pricing look inconsistent."
        });

        response.EnsureSuccessStatusCode();

        var caseId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, caseId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = await db.AuctionReports.FindAsync(caseId);
        Assert.NotNull(report);
        Assert.Equal(auctionId, report!.AuctionId);
        Assert.Equal(bidderId, report.ReportedByUserId);
        Assert.Equal("Suspicious listing", report.Reason);
        Assert.Equal("The title and pricing look inconsistent.", report.Details);
        Assert.Equal(AuctionReport.OpenStatus, report.Status);
    }
}
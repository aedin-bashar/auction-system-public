using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionSystem.IntegrationTests.Admin.ReportsAndSettings;

public class AdminModerationEndpointTests
{
    [Fact]
    public async Task ListAndResolveFlaggedCases_ReturnsPersistedCasesAndUpdatesStatus()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var now = DateTime.UtcNow;
        Guid adminId;
        Guid caseId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin, nowUtc: now.AddDays(-5));
            var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller, nowUtc: now.AddDays(-4));
            var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder, nowUtc: now.AddDays(-3));

            var auction = Auction.Create(
                seller.Id,
                "Reported Auction",
                Money.Create(200m, "USD"),
                now.AddDays(1),
                "Needs review",
                "Collectibles",
                now.AddDays(-2));
            auction.Start(now.AddDays(-2));

            db.Users.AddRange(admin, seller, bidder);
            db.Auctions.Add(auction);

            var report = new AuctionReport
            {
                Id = Guid.NewGuid(),
                AuctionId = auction.Id,
                ReportedByUserId = bidder.Id,
                Reason = "Counterfeit concern",
                Details = "Images look inconsistent with the description.",
                Status = AuctionReport.OpenStatus,
                CreatedAtUtc = now.AddMinutes(-15),
                UpdatedAtUtc = now.AddMinutes(-15)
            };

            db.AuctionReports.Add(report);
            await db.SaveChangesAsync();

            adminId = admin.Id;
            caseId = report.Id;
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var listResponse = await client.GetAsync("/api/admin/moderation/cases");
        listResponse.EnsureSuccessStatusCode();

        var cases = await listResponse.Content.ReadFromJsonAsync<List<AdminFlaggedCaseDto>>();
        Assert.NotNull(cases);
        Assert.Single(cases!);
        Assert.Equal(AuctionReport.OpenStatus, cases[0].Status);

        var resolveResponse = await client.PostAsJsonAsync($"/api/admin/moderation/cases/{caseId}/resolve", new
        {
            resolutionNote = "Reviewed and accepted as a legitimate listing."
        });
        resolveResponse.EnsureSuccessStatusCode();

        var resolved = await resolveResponse.Content.ReadFromJsonAsync<AdminFlaggedCaseDto>();
        Assert.NotNull(resolved);
        Assert.Equal(AuctionReport.ResolvedStatus, resolved!.Status);
        Assert.Equal("Admin User", resolved.ResolvedBy);

        using var verifyScope = factory.Services.CreateScope();
        var dbVerify = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await dbVerify.AuctionReports.FindAsync(caseId);
        Assert.NotNull(stored);
        Assert.Equal(AuctionReport.ResolvedStatus, stored!.Status);
        Assert.Equal("Reviewed and accepted as a legitimate listing.", stored.ResolutionNote);
        Assert.NotNull(stored.ResolvedAtUtc);
    }
}
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionSystem.IntegrationTests.Admin.ReportsAndSettings;

public class AdminDashboardEndpointTests
{
    [Fact]
    public async Task GetDashboard_ReturnsRealCountsFromPersistence()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();

        var now = DateTime.UtcNow;
        Guid adminId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin, nowUtc: now.AddDays(-10));
            var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller, nowUtc: now.AddDays(-8));
            var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder, nowUtc: now.AddDays(-6));
            var inactiveBidder = User.Register("inactive@example.com", "Inactive User", UserRole.Bidder, nowUtc: now.AddDays(-5));
            inactiveBidder.Deactivate(now.AddHours(-3));

            var auction = Auction.Create(
                seller.Id,
                "Vintage Camera",
                Money.Create(100m, "USD"),
                now.AddDays(2),
                "Collector item",
                "Tech",
                now.AddDays(-2));

            auction.Start(now.AddDays(-2));
            var previousDayBid = auction.PlaceBid(bidder.Id, Money.Create(120m, "USD"), now.AddDays(-1));
            auction.PlaceBid(bidder.Id, Money.Create(150m, "USD"), now.AddHours(-2));

            db.Users.AddRange(admin, seller, bidder, inactiveBidder);
            db.Auctions.Add(auction);
            db.AdminTransactionRefunds.Add(new AdminTransactionRefund
            {
                TransactionId = previousDayBid.Id,
                RefundedByUserId = admin.Id,
                Reason = "Customer support adjustment",
                RefundedAtUtc = now.AddHours(-1),
                CreatedAtUtc = now.AddHours(-1),
                UpdatedAtUtc = now.AddHours(-1)
            });

            db.AuctionReports.Add(new AuctionReport
            {
                Id = Guid.NewGuid(),
                AuctionId = auction.Id,
                ReportedByUserId = bidder.Id,
                Reason = "Suspicious listing",
                Details = "Price and description do not match.",
                Status = AuctionReport.OpenStatus,
                CreatedAtUtc = now.AddMinutes(-30),
                UpdatedAtUtc = now.AddMinutes(-30)
            });

            await db.SaveChangesAsync();
            adminId = admin.Id;
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.GetAsync("/api/admin/reports/dashboard");
        response.EnsureSuccessStatusCode();

        var dashboard = await response.Content.ReadFromJsonAsync<AdminDashboardDto>();

        Assert.NotNull(dashboard);
        Assert.Equal(3, dashboard!.ActiveUsers);
        Assert.Equal(1, dashboard.LiveAuctions);
        Assert.Equal(1, dashboard.DailyBids);
        Assert.Equal(1, dashboard.FlaggedCases);
        Assert.Contains(dashboard.RecentActivity, item => item.Title == "Auction reported");
        Assert.Contains(dashboard.RecentActivity, item => item.Title == "Refund processed");
        Assert.Contains(dashboard.RecentActivity, item => item.Title == "Bid placed");
    }
}
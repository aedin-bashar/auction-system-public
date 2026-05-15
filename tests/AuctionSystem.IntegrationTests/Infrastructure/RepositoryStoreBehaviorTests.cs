using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Application.Admin.SystemSettings;
using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.IntegrationTests.Infrastructure;

public class RepositoryStoreBehaviorTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AuctionSystem-RepositoryTests-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task EfRepository_GenericCrudBehavior_WorksForUsers()
    {
        await using var db = CreateDbContext();
        IRepository<User, Guid> repo = new EfRepository<User, Guid>(db);

        var user = User.Register("repo@example.com", "Repo User", UserRole.Bidder);
        await repo.AddAsync(user);
        await db.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(user.Id);
        Assert.NotNull(loaded);

        var listed = await repo.ListAsync(x => x.Email == "repo@example.com");
        Assert.Single(listed);

        user.UpdateProfile("Updated Repo User", "+1 555 111 1111");
        repo.Update(user);
        await db.SaveChangesAsync();

        var updated = await repo.GetByIdAsync(user.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Repo User", updated!.FullName);

        repo.Remove(user);
        await db.SaveChangesAsync();

        Assert.Null(await repo.GetByIdAsync(user.Id));
    }

    [Fact]
    public async Task AuctionRepository_ListActiveAsync_FiltersOrdersAndPaginates()
    {
        await using var db = CreateDbContext();
        var repo = new AuctionRepository(db);
        var now = DateTime.UtcNow;
        var sellerId = Guid.NewGuid();

        var first = Auction.Create(sellerId, "First Collectible", Money.Create(200m, "USD"), now.AddDays(1), "Desc", "Collectibles", now.AddHours(-2));
        first.Start(now.AddHours(-1));
        first.PlaceBid(Guid.NewGuid(), Money.Create(250m, "USD"), now.AddMinutes(-50));

        var second = Auction.Create(sellerId, "Second Collectible", Money.Create(210m, "USD"), now.AddDays(2), "Desc", "Collectibles", now.AddHours(-2));
        second.Start(now.AddHours(-1));
        second.PlaceBid(Guid.NewGuid(), Money.Create(260m, "USD"), now.AddMinutes(-40));

        var otherCategory = Auction.Create(sellerId, "Other Category", Money.Create(500m, "USD"), now.AddDays(3), "Desc", "Electronics", now.AddHours(-2));
        otherCategory.Start(now.AddHours(-1));

        var draft = Auction.Create(sellerId, "Draft Auction", Money.Create(220m, "USD"), now.AddDays(3), "Desc", "Collectibles", now.AddHours(-2));

        db.Auctions.AddRange(first, second, otherCategory, draft);
        await db.SaveChangesAsync();

        var firstPage = await repo.ListActiveAsync("Collectibles", 240m, 300m, pageNumber: 1, pageSize: 1);
        var secondPage = await repo.ListActiveAsync("Collectibles", 240m, 300m, pageNumber: 2, pageSize: 1);

        Assert.Single(firstPage);
        Assert.Single(secondPage);
        Assert.Equal(first.Id, firstPage[0].Id);
        Assert.Equal(second.Id, secondPage[0].Id);
        Assert.All(firstPage.Concat(secondPage), x => Assert.Equal("Collectibles", x.Category));
    }

    [Fact]
    public async Task PaymentMethodStore_AddGetAndRemove_ManagesDefaultsAndOrdering()
    {
        await using var db = CreateDbContext();
        var store = new PaymentMethodStore(db);
        var userId = Guid.NewGuid();

        var first = await store.AddAsync(new AddPaymentMethodRequest(userId, "Card", "Visa", "1111", 10, DateTime.UtcNow.Year + 1, "User One", true));
        await db.SaveChangesAsync();

        var second = await store.AddAsync(new AddPaymentMethodRequest(userId, "Card", "Mastercard", "2222", 11, DateTime.UtcNow.Year + 2, "User One", true));
        await db.SaveChangesAsync();

        var methods = await store.GetByUserIdAsync(userId);

        Assert.Equal(2, methods.Count);
        Assert.Equal(second.Id, methods[0].Id);
        Assert.True(methods[0].IsDefault);
        Assert.False(methods[1].IsDefault);

        var removed = await store.RemoveAsync(userId, second.Id);
        await db.SaveChangesAsync();
        var missing = await store.RemoveAsync(userId, Guid.NewGuid());

        Assert.True(removed);
        Assert.False(missing);
        Assert.Single(await store.GetByUserIdAsync(userId));
    }

    [Fact]
    public async Task AuctionReportStore_CreateListResolveAndHasOpenCase_Work()
    {
        await using var db = CreateDbContext();
        var store = new AuctionReportStore(db);
        var now = DateTime.UtcNow;

        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller, nowUtc: now.AddDays(-2));
        var reporter = User.Register("reporter@example.com", "Reporter User", UserRole.Bidder, nowUtc: now.AddDays(-1));
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin, nowUtc: now.AddDays(-3));
        var auction = Auction.Create(seller.Id, "Flagged Auction", Money.Create(100m, "USD"), now.AddDays(2), "Desc", "Collectibles", now.AddHours(-3));
        auction.Start(now.AddHours(-2));

        db.Users.AddRange(seller, reporter, admin);
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();

        var caseId = await store.CreateAsync(new CreateAuctionReportRequest(auction.Id, reporter.Id, "  Fraud  ", "Suspicious listing", now));
        await db.SaveChangesAsync();

        Assert.True(await store.HasOpenCaseAsync(auction.Id, reporter.Id));

        var openCases = await store.ListAsync(includeResolved: false);
        Assert.Single(openCases);
        Assert.Equal("Fraud", openCases[0].Reason);

        var resolved = await store.ResolveAsync(new ResolveAuctionReportRequest(caseId, admin.Id, "Reviewed and closed", now.AddMinutes(5)));
        await db.SaveChangesAsync();

        Assert.NotNull(resolved);
        Assert.Equal(AuctionReport.ResolvedStatus, resolved!.Status);
        Assert.Equal("Admin User", resolved.ResolvedBy);

        var allCases = await store.ListAsync(includeResolved: true);
        Assert.Single(allCases);
        Assert.Equal(AuctionReport.ResolvedStatus, allCases[0].Status);
    }

    [Fact]
    public async Task AdminSystemSettingsStore_Upsert_CreatesAndUpdatesSetting()
    {
        await using var db = CreateDbContext();
        var store = new AdminSystemSettingsStore(db);
        var adminId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var created = await store.UpsertAsync(new UpsertAdminSystemSettingRequest("  maintenance.mode  ", " true ", adminId, now));
        var updated = await store.UpsertAsync(new UpsertAdminSystemSettingRequest("maintenance.mode", " false ", adminId, now.AddMinutes(1)));

        Assert.Equal("maintenance.mode", created.Key);
        Assert.Equal("true", created.Value);
        Assert.Equal("false", updated.Value);
        Assert.Single(await db.AdminSystemSettings.ToListAsync());
    }

    [Fact]
    public async Task AdminTransactionStore_ListGetAndProcessRefund_Work()
    {
        await using var db = CreateDbContext();
        var store = new AdminTransactionStore(db);
        var now = DateTime.UtcNow;

        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin, nowUtc: now.AddDays(-3));
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller, nowUtc: now.AddDays(-2));
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder, nowUtc: now.AddDays(-1));
        var auction = Auction.Create(seller.Id, "Transaction Auction", Money.Create(100m, "USD"), now.AddDays(2), "Desc", "Collectibles", now.AddHours(-3));
        auction.Start(now.AddHours(-2));
        var bid = auction.PlaceBid(bidder.Id, Money.Create(175m, "USD"), now.AddHours(-1));

        db.Users.AddRange(admin, seller, bidder);
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();

        var listed = await store.ListAsync();
        Assert.Single(listed);
        Assert.Equal("Completed", listed[0].Status);

        var detail = await store.GetByIdAsync(bid.Id);
        Assert.NotNull(detail);
        Assert.Equal(-175m, detail!.WalletBalanceAmount);

        var refunded = await store.ProcessRefundAsync(new ProcessAdminRefundRequest(bid.Id, admin.Id, "Duplicate charge", now));
        await db.SaveChangesAsync();
        var reloaded = await store.GetByIdAsync(bid.Id);

        Assert.NotNull(refunded);
        Assert.Equal("Refunded", refunded!.Status);
        Assert.Equal("Duplicate charge", refunded.RefundReason);
        Assert.Equal(175m, refunded.WalletBalanceAmount);
        Assert.NotNull(reloaded);
        Assert.Equal("Refunded", reloaded!.Status);
    }

    [Fact]
    public async Task AdminReportStore_DashboardAndGenerate_ReturnExpectedAggregates()
    {
        await using var db = CreateDbContext();
        var store = new AdminReportStore(db);
        var now = DateTime.UtcNow;

        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin, nowUtc: now.AddDays(-3));
        var seller = User.Register("seller@example.com", "Seller User", UserRole.Seller, nowUtc: now.AddDays(-2));
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder, nowUtc: now.AddDays(-1));
        var auction = Auction.Create(seller.Id, "Dashboard Auction", Money.Create(100m, "USD"), now.AddDays(2), "Desc", "Collectibles", now.AddHours(-4));
        auction.Start(now.AddHours(-3));
        var bid = auction.PlaceBid(bidder.Id, Money.Create(200m, "USD"), now.AddHours(-1));

        db.Users.AddRange(admin, seller, bidder);
        db.Auctions.Add(auction);
        db.AuctionReports.Add(new AuctionReport
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            ReportedByUserId = bidder.Id,
            Reason = "Fraud",
            Status = AuctionReport.OpenStatus,
            CreatedAtUtc = now.AddMinutes(-30),
            UpdatedAtUtc = now.AddMinutes(-30)
        });
        db.AdminTransactionRefunds.Add(new AdminTransactionRefund
        {
            TransactionId = bid.Id,
            RefundedByUserId = admin.Id,
            Reason = "Duplicate charge",
            RefundedAtUtc = now.AddMinutes(-10),
            CreatedAtUtc = now.AddMinutes(-10),
            UpdatedAtUtc = now.AddMinutes(-10)
        });
        await db.SaveChangesAsync();

        var dashboard = await store.GetDashboardAsync(now);
        var report = await store.GenerateAsync(new GenerateAdminReportRequest("overview", now.AddDays(-7), now, now, admin.Id));

        Assert.Equal(3, dashboard.ActiveUsers);
        Assert.Equal(1, dashboard.LiveAuctions);
        Assert.Equal(1, dashboard.DailyBids);
        Assert.Equal(1, dashboard.FlaggedCases);
        Assert.NotEmpty(dashboard.RecentActivity);

        Assert.NotNull(report);
        Assert.Equal(1, report!.Totals["auctions"]);
        Assert.Equal(1, report.Totals["bids"]);
        Assert.Equal(3, report.Totals["activeUsers"]);
        Assert.Equal(1, report.Totals["refunds"]);
        Assert.Equal(200m, report.Metrics["bidVolume"]);
        Assert.Equal(200m, report.Metrics["averageBid"]);
        Assert.Equal(200m, report.Metrics["refundValue"]);
    }
}
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using AuctionSystem.IntegrationTests.Admin.ReportsAndSettings;
using AuctionSystem.IntegrationTests.Infrastructure;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionSystem.IntegrationTests.Admin.Transactions;

public sealed class AdminTransactionEndpointTests
{
    [Fact]
    public async Task GetTransactions_WhenRequesterIsAdmin_ReturnsTransactions()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var sellerId = await factory.SeedUserAsync("seller@example.com", "Seller User", UserRole.Seller);
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);
        var (transactionId, _) = await SeedBidTransactionAsync(factory, sellerId, bidderId, 150m, "USD");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.GetAsync("/api/admin/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<IReadOnlyList<AdminTransactionListItemDto>>();
        Assert.NotNull(transactions);
        Assert.Contains(transactions!, x => x.TransactionId == transactionId && x.Status == "Completed");
    }

    [Fact]
    public async Task GetTransaction_WhenRequesterIsAdmin_ReturnsTransactionDetail()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var sellerId = await factory.SeedUserAsync("seller@example.com", "Seller User", UserRole.Seller);
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);
        var (transactionId, auctionId) = await SeedBidTransactionAsync(factory, sellerId, bidderId, 175m, "USD");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.GetAsync($"/api/admin/transactions/{transactionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transaction = await response.Content.ReadFromJsonAsync<AdminTransactionDetailDto>();
        Assert.NotNull(transaction);
        Assert.Equal(transactionId, transaction!.TransactionId);
        Assert.Equal(bidderId, transaction.UserId);
        Assert.Equal("Completed", transaction.Status);
        Assert.Contains(auctionId.ToString("N")[..8], transaction.Reference!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefundTransaction_WhenRequesterIsAdmin_ProcessesRefund()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var adminId = await factory.SeedUserAsync("admin@example.com", "Admin User", UserRole.Admin);
        var sellerId = await factory.SeedUserAsync("seller@example.com", "Seller User", UserRole.Seller);
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);
        var (transactionId, _) = await SeedBidTransactionAsync(factory, sellerId, bidderId, 200m, "USD");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(adminId, "admin@example.com", UserRole.Admin.ToString()));

        var response = await client.PostAsJsonAsync($"/api/admin/transactions/{transactionId}/refund", new
        {
            Reason = "Duplicate charge"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transaction = await response.Content.ReadFromJsonAsync<AdminTransactionDetailDto>();
        Assert.NotNull(transaction);
        Assert.Equal("Refunded", transaction!.Status);
        Assert.Equal("Duplicate charge", transaction.RefundReason);
        Assert.Equal("Admin User", transaction.RefundedBy);
        Assert.NotNull(transaction.RefundedAtUtc);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var refund = await db.AdminTransactionRefunds.SingleAsync(x => x.TransactionId == transactionId);

        Assert.Equal(adminId, refund.RefundedByUserId);
        Assert.Equal("Duplicate charge", refund.Reason);
    }

    [Fact]
    public async Task GetTransactions_WhenRequesterIsNotAdmin_ReturnsForbidden()
    {
        await using var factory = new AdminReportsAndSettingsWebApplicationFactory();
        var bidderId = await factory.SeedUserAsync("bidder@example.com", "Bidder User", UserRole.Bidder);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(bidderId, "bidder@example.com", UserRole.Bidder.ToString()));

        var response = await client.GetAsync("/api/admin/transactions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<(Guid TransactionId, Guid AuctionId)> SeedBidTransactionAsync(
        AdminReportsAndSettingsWebApplicationFactory factory,
        Guid sellerId,
        Guid bidderId,
        decimal amount,
        string currency)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var auction = Auction.Create(
            sellerId,
            "Transaction Auction",
            Money.Create(100m, currency),
            DateTime.UtcNow.AddDays(5),
            "Seeded description",
            "Collectibles",
            DateTime.UtcNow.AddHours(-1));

        auction.Start(DateTime.UtcNow.AddMinutes(-30));
        var bid = auction.PlaceBid(bidderId, Money.Create(amount, currency), DateTime.UtcNow.AddMinutes(-10));

        db.Auctions.Add(auction);
        await db.SaveChangesAsync();

        return (bid.Id, auction.Id);
    }
}
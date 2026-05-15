using System.Globalization;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Repositories;

public sealed class AdminReportStore : IAdminReportStore
{
    private readonly ApplicationDbContext _db;

    public AdminReportStore(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var now = EnsureUtc(nowUtc);
        var startOfDayUtc = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        var activeUsers = await _db.Users
            .AsNoTracking()
            .CountAsync(x => x.IsActive, cancellationToken);

        var liveAuctions = await _db.Auctions
            .AsNoTracking()
            .CountAsync(x => x.Status == AuctionStatus.Active && x.EndTimeUtc > now, cancellationToken);

        var dailyBids = await _db.Bids
            .AsNoTracking()
            .CountAsync(x => x.PlacedAtUtc >= startOfDayUtc && x.PlacedAtUtc <= now, cancellationToken);

        var flaggedCases = await _db.AuctionReports
            .AsNoTracking()
            .CountAsync(x => x.Status == AuctionReport.OpenStatus, cancellationToken);

        var refundedTransactions = await _db.AdminTransactionRefunds
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var recentUsers = await _db.Users
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.FullName, x.Role, x.CreatedAtUtc })
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentAuctions = await _db.Auctions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Title, x.Category, x.CreatedAtUtc })
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentBids = await (
            from bid in _db.Bids.AsNoTracking()
            join auction in _db.Auctions.AsNoTracking() on bid.AuctionId equals auction.Id
            orderby bid.PlacedAtUtc descending
            select new
            {
                AuctionTitle = auction.Title,
                Amount = bid.Amount.Amount,
                Currency = bid.Amount.Currency,
                bid.PlacedAtUtc
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentRefunds = await (
            from refund in _db.AdminTransactionRefunds.AsNoTracking()
            join bid in _db.Bids.AsNoTracking() on refund.TransactionId equals bid.Id
            join auction in _db.Auctions.AsNoTracking() on bid.AuctionId equals auction.Id
            orderby refund.RefundedAtUtc descending
            select new
            {
                AuctionTitle = auction.Title,
                refund.RefundedAtUtc
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentReports = await (
            from report in _db.AuctionReports.AsNoTracking()
            join auction in _db.Auctions.AsNoTracking() on report.AuctionId equals auction.Id
            join reporter in _db.Users.AsNoTracking() on report.ReportedByUserId equals reporter.Id
            orderby report.CreatedAtUtc descending
            select new
            {
                AuctionTitle = auction.Title,
                ReporterName = reporter.FullName,
                report.Reason,
                report.CreatedAtUtc
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentResolvedReports = await (
            from report in _db.AuctionReports.AsNoTracking()
            where report.Status == AuctionReport.ResolvedStatus && report.ResolvedAtUtc != null && report.ResolvedByUserId != null
            join auction in _db.Auctions.AsNoTracking() on report.AuctionId equals auction.Id
            join resolvedBy in _db.Users.AsNoTracking() on report.ResolvedByUserId equals resolvedBy.Id
            orderby report.ResolvedAtUtc descending
            select new
            {
                AuctionTitle = auction.Title,
                ResolvedBy = resolvedBy.FullName,
                ResolvedAtUtc = report.ResolvedAtUtc!.Value
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentActivity = recentUsers
            .Select(x => new AdminDashboardActivityDto(
                "user-registered",
                "New user registered",
                $"{x.FullName} joined as {x.Role}.",
                x.CreatedAtUtc))
            .Concat(recentAuctions.Select(x => new AdminDashboardActivityDto(
                "auction-listed",
                "Auction listed",
                $"\"{x.Title}\" was created in {x.Category}.",
                x.CreatedAtUtc)))
            .Concat(recentBids.Select(x => new AdminDashboardActivityDto(
                "bid-placed",
                "Bid placed",
                $"{FormatAmount(x.Amount, x.Currency)} bid on \"{x.AuctionTitle}\".",
                x.PlacedAtUtc)))
            .Concat(recentReports.Select(x => new AdminDashboardActivityDto(
                "auction-reported",
                "Auction reported",
                $"{x.ReporterName} flagged \"{x.AuctionTitle}\" for {x.Reason.ToLowerInvariant()}.",
                x.CreatedAtUtc)))
            .Concat(recentResolvedReports.Select(x => new AdminDashboardActivityDto(
                "report-resolved",
                "Flagged case resolved",
                $"{x.ResolvedBy} resolved a report on \"{x.AuctionTitle}\".",
                x.ResolvedAtUtc)))
            .Concat(recentRefunds.Select(x => new AdminDashboardActivityDto(
                "refund-processed",
                "Refund processed",
                $"Refund recorded for \"{x.AuctionTitle}\".",
                x.RefundedAtUtc)))
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(8)
            .ToList();

        return new AdminDashboardDto(
            now,
            activeUsers,
            liveAuctions,
            dailyBids,
            flaggedCases,
            recentActivity);
    }

    public async Task<AdminReportDto?> GenerateAsync(GenerateAdminReportRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.ReportType)) throw new ArgumentException("Report type is required.", nameof(request));

        var rangeStart = request.RangeStartUtc.Kind == DateTimeKind.Utc
            ? request.RangeStartUtc
            : request.RangeStartUtc.ToUniversalTime();

        var rangeEnd = request.RangeEndUtc.Kind == DateTimeKind.Utc
            ? request.RangeEndUtc
            : request.RangeEndUtc.ToUniversalTime();

        var auctionsInRange = _db.Auctions
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= rangeStart && x.CreatedAtUtc <= rangeEnd);

        var bidsInRange = _db.Bids
            .AsNoTracking()
            .Where(x => x.PlacedAtUtc >= rangeStart && x.PlacedAtUtc <= rangeEnd);

        var activeUsersInRange = _db.Users
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= rangeStart && x.CreatedAtUtc <= rangeEnd && x.IsActive);

        var refundsInRange = _db.AdminTransactionRefunds
            .AsNoTracking()
            .Where(x => x.RefundedAtUtc >= rangeStart && x.RefundedAtUtc <= rangeEnd);

        var totalAuctionCount = await auctionsInRange.CountAsync(cancellationToken);
        var totalBidCount = await bidsInRange.CountAsync(cancellationToken);
        var totalActiveUserCount = await activeUsersInRange.CountAsync(cancellationToken);
        var totalRefundCount = await refundsInRange.CountAsync(cancellationToken);

        var totalBidVolume = await bidsInRange
            .Select(x => (decimal?)x.Amount.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var averageBidAmount = totalBidCount == 0
            ? 0m
            : decimal.Round(totalBidVolume / totalBidCount, 2);

        var refundValue = await (
            from refund in refundsInRange
            join bid in _db.Bids.AsNoTracking() on refund.TransactionId equals bid.Id
            select (decimal?)bid.Amount.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var metrics = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["bidVolume"] = decimal.Round(totalBidVolume, 2),
            ["averageBid"] = averageBidAmount,
            ["refundValue"] = decimal.Round(refundValue, 2)
        };

        var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["auctions"] = totalAuctionCount,
            ["bids"] = totalBidCount,
            ["activeUsers"] = totalActiveUserCount,
            ["refunds"] = totalRefundCount
        };

        return new AdminReportDto(
            request.ReportType.Trim(),
            rangeStart,
            rangeEnd,
            request.RequestedAtUtc,
            metrics,
            totals);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value;
    }

    private static string FormatAmount(decimal amount, string currency)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{decimal.Round(amount, 2):0.##} {currency}");
    }
}

using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Repositories;

public sealed class AuctionReportStore : IAuctionReportStore
{
    private readonly ApplicationDbContext _db;

    public AuctionReportStore(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<bool> HasOpenCaseAsync(Guid auctionId, Guid reportedByUserId, CancellationToken cancellationToken = default)
    {
        return _db.AuctionReports
            .AsNoTracking()
            .AnyAsync(
                x => x.AuctionId == auctionId
                    && x.ReportedByUserId == reportedByUserId
                    && x.Status == AuctionReport.OpenStatus,
                cancellationToken);
    }

    public async Task<Guid> CreateAsync(CreateAuctionReportRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var now = EnsureUtc(request.RequestedAtUtc);
        var report = new AuctionReport
        {
            Id = Guid.NewGuid(),
            AuctionId = request.AuctionId,
            ReportedByUserId = request.ReportedByUserId,
            Reason = request.Reason.Trim(),
            Details = request.Details,
            Status = AuctionReport.OpenStatus,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _db.AuctionReports.AddAsync(report, cancellationToken);
        return report.Id;
    }

    public async Task<IReadOnlyList<AdminFlaggedCaseDto>> ListAsync(bool includeResolved, CancellationToken cancellationToken = default)
    {
        var reports =
            from report in _db.AuctionReports.AsNoTracking()
            join auction in _db.Auctions.AsNoTracking() on report.AuctionId equals auction.Id
            join reporter in _db.Users.AsNoTracking() on report.ReportedByUserId equals reporter.Id
            join resolvedBy in _db.Users.AsNoTracking() on report.ResolvedByUserId equals resolvedBy.Id into resolvedByJoin
            from resolvedBy in resolvedByJoin.DefaultIfEmpty()
            select new
            {
                Report = report,
                Auction = auction,
                Reporter = reporter,
                ResolvedBy = resolvedBy
            };

        if (!includeResolved)
        {
            reports = reports.Where(x => x.Report.Status == AuctionReport.OpenStatus);
        }

        return await reports
            .OrderBy(x => x.Report.Status == AuctionReport.OpenStatus ? 0 : 1)
            .ThenByDescending(x => x.Report.UpdatedAtUtc)
            .Select(x => new AdminFlaggedCaseDto(
                x.Report.Id,
                x.Auction.Id,
                x.Auction.Title,
                x.Reporter.Id,
                x.Reporter.FullName,
                x.Report.Reason,
                x.Report.Details,
                x.Report.Status,
                x.Report.CreatedAtUtc,
                x.Report.UpdatedAtUtc,
                x.Report.ResolvedAtUtc,
                x.ResolvedBy == null ? null : x.ResolvedBy.FullName,
                x.Report.ResolutionNote))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminFlaggedCaseDto?> ResolveAsync(ResolveAuctionReportRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var report = await _db.AuctionReports
            .SingleOrDefaultAsync(x => x.Id == request.CaseId, cancellationToken);

        if (report is null)
        {
            return null;
        }

        var now = EnsureUtc(request.RequestedAtUtc);
        report.Status = AuctionReport.ResolvedStatus;
        report.ResolutionNote = request.ResolutionNote;
        report.ResolvedByUserId = request.ResolvedByUserId;
        report.ResolvedAtUtc = now;
        report.UpdatedAtUtc = now;

        var auctionTitle = await _db.Auctions
            .AsNoTracking()
            .Where(x => x.Id == report.AuctionId)
            .Select(x => x.Title)
            .SingleOrDefaultAsync(cancellationToken);

        var reporterName = await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == report.ReportedByUserId)
            .Select(x => x.FullName)
            .SingleOrDefaultAsync(cancellationToken);

        var resolvedBy = await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == request.ResolvedByUserId)
            .Select(x => x.FullName)
            .SingleOrDefaultAsync(cancellationToken);

        return new AdminFlaggedCaseDto(
            report.Id,
            report.AuctionId,
            auctionTitle ?? "Unknown Auction",
            report.ReportedByUserId,
            reporterName ?? "Unknown User",
            report.Reason,
            report.Details,
            report.Status,
            report.CreatedAtUtc,
            report.UpdatedAtUtc,
            report.ResolvedAtUtc,
            resolvedBy,
            report.ResolutionNote);
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
}
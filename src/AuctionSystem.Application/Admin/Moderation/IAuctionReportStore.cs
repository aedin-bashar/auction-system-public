namespace AuctionSystem.Application.Admin.Moderation;

public interface IAuctionReportStore
{
    Task<bool> HasOpenCaseAsync(Guid auctionId, Guid reportedByUserId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CreateAuctionReportRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminFlaggedCaseDto>> ListAsync(bool includeResolved, CancellationToken cancellationToken = default);
    Task<AdminFlaggedCaseDto?> ResolveAsync(ResolveAuctionReportRequest request, CancellationToken cancellationToken = default);
}

public sealed record CreateAuctionReportRequest(
    Guid AuctionId,
    Guid ReportedByUserId,
    string Reason,
    string? Details,
    DateTime RequestedAtUtc);

public sealed record ResolveAuctionReportRequest(
    Guid CaseId,
    Guid ResolvedByUserId,
    string? ResolutionNote,
    DateTime RequestedAtUtc);
namespace AuctionSystem.Application.Admin.Reports;

public interface IAdminReportStore
{
    Task<AdminDashboardDto> GetDashboardAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<AdminReportDto?> GenerateAsync(GenerateAdminReportRequest request, CancellationToken cancellationToken = default);
}

public sealed record GenerateAdminReportRequest(
    string ReportType,
    DateTime RangeStartUtc,
    DateTime RangeEndUtc,
    DateTime RequestedAtUtc,
    Guid RequestedByUserId);

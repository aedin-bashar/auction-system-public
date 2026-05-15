namespace AuctionSystem.Application.Admin.Reports;

public sealed record AdminReportDto(
    string ReportType,
    DateTime RangeStartUtc,
    DateTime RangeEndUtc,
    DateTime GeneratedAtUtc,
    IReadOnlyDictionary<string, decimal> Metrics,
    IReadOnlyDictionary<string, int> Totals);

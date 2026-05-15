using MediatR;

namespace AuctionSystem.Application.Admin.Reports.GenerateAdminReport;

public sealed record GenerateAdminReportCommand(
    Guid RequesterUserId,
    string ReportType,
    DateTime RangeStartUtc,
    DateTime RangeEndUtc) : IRequest<AdminReportDto>;

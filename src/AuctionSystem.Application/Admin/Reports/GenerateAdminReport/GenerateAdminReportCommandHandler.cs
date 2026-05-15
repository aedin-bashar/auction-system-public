using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Admin.Reports.GenerateAdminReport;

public sealed class GenerateAdminReportCommandHandler : IRequestHandler<GenerateAdminReportCommand, AdminReportDto>
{
    private readonly IUserRepository _users;
    private readonly IAdminReportStore _reports;

    public GenerateAdminReportCommandHandler(IUserRepository users, IAdminReportStore reports)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public async Task<AdminReportDto> Handle(GenerateAdminReportCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can generate reports.");
        }

        var report = await _reports.GenerateAsync(
            new GenerateAdminReportRequest(
                request.ReportType.Trim(),
                request.RangeStartUtc,
                request.RangeEndUtc,
                DateTime.UtcNow,
                request.RequesterUserId),
            cancellationToken);

        if (report is null)
        {
            throw new InvalidOperationException("Report generation failed.");
        }

        return report;
    }
}

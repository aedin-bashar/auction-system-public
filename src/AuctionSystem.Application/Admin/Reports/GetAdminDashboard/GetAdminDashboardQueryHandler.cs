using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Admin.Reports.GetAdminDashboard;

public sealed class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IUserRepository _users;
    private readonly IAdminReportStore _reports;

    public GetAdminDashboardQueryHandler(IUserRepository users, IAdminReportStore reports)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can view the admin dashboard.");
        }

        return await _reports.GetDashboardAsync(DateTime.UtcNow, cancellationToken);
    }
}
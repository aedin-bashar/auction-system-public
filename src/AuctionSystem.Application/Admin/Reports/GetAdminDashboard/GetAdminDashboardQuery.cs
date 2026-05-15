using MediatR;

namespace AuctionSystem.Application.Admin.Reports.GetAdminDashboard;

public sealed record GetAdminDashboardQuery(Guid RequesterUserId) : IRequest<AdminDashboardDto>;
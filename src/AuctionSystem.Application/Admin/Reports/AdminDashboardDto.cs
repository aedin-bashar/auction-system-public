namespace AuctionSystem.Application.Admin.Reports;

public sealed record AdminDashboardDto(
    DateTime GeneratedAtUtc,
    int ActiveUsers,
    int LiveAuctions,
    int DailyBids,
    int FlaggedCases,
    IReadOnlyList<AdminDashboardActivityDto> RecentActivity);

public sealed record AdminDashboardActivityDto(
    string Kind,
    string Title,
    string Description,
    DateTime OccurredAtUtc);
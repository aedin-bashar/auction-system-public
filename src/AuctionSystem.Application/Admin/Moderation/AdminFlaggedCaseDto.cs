namespace AuctionSystem.Application.Admin.Moderation;

public sealed record AdminFlaggedCaseDto(
    Guid CaseId,
    Guid AuctionId,
    string AuctionTitle,
    Guid ReportedByUserId,
    string ReporterName,
    string Reason,
    string? Details,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ResolvedAtUtc,
    string? ResolvedBy,
    string? ResolutionNote);
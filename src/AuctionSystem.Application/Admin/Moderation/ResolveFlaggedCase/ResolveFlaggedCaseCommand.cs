using MediatR;

namespace AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase;

public sealed record ResolveFlaggedCaseCommand(
    Guid RequesterUserId,
    Guid CaseId,
    string? ResolutionNote) : IRequest<AdminFlaggedCaseDto>;
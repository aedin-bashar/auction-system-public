using MediatR;

namespace AuctionSystem.Application.Admin.Moderation.GetFlaggedCases;

public sealed record GetFlaggedCasesQuery(Guid RequesterUserId, bool IncludeResolved = false)
    : IRequest<IReadOnlyList<AdminFlaggedCaseDto>>;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Admin.Moderation.GetFlaggedCases;

public sealed class GetFlaggedCasesQueryHandler : IRequestHandler<GetFlaggedCasesQuery, IReadOnlyList<AdminFlaggedCaseDto>>
{
    private readonly IUserRepository _users;
    private readonly IAuctionReportStore _reports;

    public GetFlaggedCasesQueryHandler(IUserRepository users, IAuctionReportStore reports)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public async Task<IReadOnlyList<AdminFlaggedCaseDto>> Handle(GetFlaggedCasesQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can view flagged cases.");
        }

        return await _reports.ListAsync(request.IncludeResolved, cancellationToken);
    }
}
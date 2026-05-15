using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase;

public sealed class ResolveFlaggedCaseCommandHandler : IRequestHandler<ResolveFlaggedCaseCommand, AdminFlaggedCaseDto>
{
    private readonly IUserRepository _users;
    private readonly IAuctionReportStore _reports;
    private readonly IUnitOfWork _unitOfWork;

    public ResolveFlaggedCaseCommandHandler(
        IUserRepository users,
        IAuctionReportStore reports,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AdminFlaggedCaseDto> Handle(ResolveFlaggedCaseCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can resolve flagged cases.");
        }

        var result = await _reports.ResolveAsync(
            new ResolveAuctionReportRequest(
                request.CaseId,
                request.RequesterUserId,
                string.IsNullOrWhiteSpace(request.ResolutionNote) ? null : request.ResolutionNote.Trim(),
                DateTime.UtcNow),
            cancellationToken);

        if (result is null)
        {
            throw new KeyNotFoundException("Flagged case was not found.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
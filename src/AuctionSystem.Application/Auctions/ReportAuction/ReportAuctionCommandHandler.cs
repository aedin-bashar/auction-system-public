using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Auctions.ReportAuction;

public sealed class ReportAuctionCommandHandler : IRequestHandler<ReportAuctionCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly IAuctionRepository _auctions;
    private readonly IAuctionReportStore _reports;
    private readonly IUnitOfWork _unitOfWork;

    public ReportAuctionCommandHandler(
        IUserRepository users,
        IAuctionRepository auctions,
        IAuctionReportStore reports,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Guid> Handle(ReportAuctionCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var reporter = await _users.GetByIdAsync(request.ReportedByUserId, cancellationToken);
        if (reporter is null)
        {
            throw new KeyNotFoundException("Reporting user was not found.");
        }

        if (!reporter.IsActive)
        {
            throw new InvalidOperationException("Inactive users cannot report auctions.");
        }

        var auction = await _auctions.GetByIdAsync(request.AuctionId, cancellationToken);
        if (auction is null)
        {
            throw new KeyNotFoundException("Auction was not found.");
        }

        if (auction.SellerId == reporter.Id)
        {
            throw new InvalidOperationException("You cannot report your own auction.");
        }

        var hasOpenCase = await _reports.HasOpenCaseAsync(request.AuctionId, request.ReportedByUserId, cancellationToken);
        if (hasOpenCase)
        {
            throw new InvalidOperationException("You already have an open report for this auction.");
        }

        var caseId = await _reports.CreateAsync(
            new CreateAuctionReportRequest(
                request.AuctionId,
                request.ReportedByUserId,
                request.Reason.Trim(),
                string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(),
                DateTime.UtcNow),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return caseId;
    }
}
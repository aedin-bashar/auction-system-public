using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin;

public sealed class EndAuctionByAdminCommandHandler : IRequestHandler<EndAuctionByAdminCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IAuctionRepository _auctions;
    private readonly IUnitOfWork _unitOfWork;

    public EndAuctionByAdminCommandHandler(
        IUserRepository users,
        IAuctionRepository auctions,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Unit> Handle(EndAuctionByAdminCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can manage auctions.");
        }

        var auction = await _auctions.GetByIdAsync(request.AuctionId, cancellationToken);
        if (auction is null)
        {
            throw new KeyNotFoundException("Auction was not found.");
        }

        if (auction.Status != AuctionStatus.Active)
        {
            throw new InvalidOperationException("Only active auctions can be ended by an administrator.");
        }

        auction.End();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
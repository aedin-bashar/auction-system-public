using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.StartAuctionByAdmin;

public sealed class StartAuctionByAdminCommandHandler : IRequestHandler<StartAuctionByAdminCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IAuctionRepository _auctions;
    private readonly IUnitOfWork _unitOfWork;

    public StartAuctionByAdminCommandHandler(
        IUserRepository users,
        IAuctionRepository auctions,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Unit> Handle(StartAuctionByAdminCommand request, CancellationToken cancellationToken)
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

        if (auction.Status != AuctionStatus.Draft)
        {
            throw new InvalidOperationException("Only draft auctions can be started by an administrator.");
        }

        auction.Start();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

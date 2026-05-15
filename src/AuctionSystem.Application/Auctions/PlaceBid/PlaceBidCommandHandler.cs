using AuctionSystem.Application.Abstractions.Realtime;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using MediatR;

namespace AuctionSystem.Application.Auctions.PlaceBid;

public sealed class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, PlaceBidResultDto>
{
    private readonly IAuctionRepository _auctions;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuctionRealtimeNotifier _realtimeNotifier;

    public PlaceBidCommandHandler(
        IAuctionRepository auctions,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IAuctionRealtimeNotifier realtimeNotifier)
    {
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _realtimeNotifier = realtimeNotifier ?? throw new ArgumentNullException(nameof(realtimeNotifier));
    }

    public async Task<PlaceBidResultDto> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var bidder = await _users.GetByIdAsync(request.BidderId, cancellationToken);
        if (bidder is null)
        {
            throw new KeyNotFoundException("Bidder was not found.");
        }

        if (!bidder.IsActive)
        {
            throw new InvalidOperationException("Inactive users cannot place bids.");
        }

        if (bidder.Role != UserRole.Bidder)
        {
            throw new InvalidOperationException("Only users with Bidder role can place bids.");
        }

        var auction = await _auctions.GetWithBidsByIdAsync(request.AuctionId, cancellationToken);
        if (auction is null)
        {
            throw new KeyNotFoundException("Auction was not found.");
        }

        var bidAmount = Money.Create(request.Amount, request.Currency);
        var bid = auction.PlaceBid(request.BidderId, bidAmount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _realtimeNotifier.NotifyBidPlacedAsync(
            new BidPlacedRealtimeEvent(
                auction.Id,
                bid.Id,
                bid.BidderId,
                bid.Amount.Amount,
                bid.Amount.Currency,
                bid.PlacedAtUtc,
                auction.CurrentPrice.Amount,
                auction.CurrentPrice.Currency),
            cancellationToken);

        return new PlaceBidResultDto(
            bid.Id,
            bid.AuctionId,
            bid.BidderId,
            bid.Amount.Amount,
            bid.Amount.Currency,
            bid.PlacedAtUtc,
            auction.CurrentPrice.Amount,
            auction.CurrentPrice.Currency);
    }
}
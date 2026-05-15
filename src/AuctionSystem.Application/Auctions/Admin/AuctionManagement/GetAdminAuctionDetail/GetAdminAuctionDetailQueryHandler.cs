using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctionDetail;

public sealed class GetAdminAuctionDetailQueryHandler : IRequestHandler<GetAdminAuctionDetailQuery, AdminAuctionDetailDto>
{
    private readonly IUserRepository _users;
    private readonly IAuctionRepository _auctions;

    public GetAdminAuctionDetailQueryHandler(IUserRepository users, IAuctionRepository auctions)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
    }

    public async Task<AdminAuctionDetailDto> Handle(GetAdminAuctionDetailQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        await EnsureRequesterIsActiveAdminAsync(request.RequesterUserId, cancellationToken);

        var auction = await _auctions.GetWithBidsByIdAsync(request.AuctionId, cancellationToken);
        if (auction is null)
        {
            throw new KeyNotFoundException("Auction was not found.");
        }

        var participantIds = auction.Bids.Select(x => x.BidderId)
            .Append(auction.SellerId)
            .Distinct()
            .ToArray();

        var users = await _users.ListAsync(user => participantIds.Contains(user.Id), cancellationToken);
        var usersById = users.ToDictionary(x => x.Id, x => x.FullName);

        var orderedBids = auction.Bids
            .OrderByDescending(x => x.PlacedAtUtc)
            .Select(x => new AdminAuctionBidDto(
                x.Id,
                x.BidderId,
                usersById.GetValueOrDefault(x.BidderId, "Unknown Bidder"),
                x.Amount.Amount,
                x.Amount.Currency,
                x.PlacedAtUtc))
            .ToList();

        var highestBid = auction.Bids
            .OrderByDescending(x => x.Amount.Amount)
            .ThenByDescending(x => x.PlacedAtUtc)
            .FirstOrDefault();

        var primaryImageId = auction.Images
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefault();

        return new AdminAuctionDetailDto(
            auction.Id,
            auction.Title,
            auction.SellerId,
            usersById.GetValueOrDefault(auction.SellerId, "Unknown Seller"),
            auction.Category,
            auction.Description,
            auction.StartingPrice.Amount,
            auction.StartingPrice.Currency,
            auction.CurrentPrice.Amount,
            auction.Bids.Count,
            auction.StartTimeUtc,
            auction.EndTimeUtc,
            auction.EndedAtUtc,
            auction.Status.ToString(),
            highestBid is null ? null : usersById.GetValueOrDefault(highestBid.BidderId, "Unknown Bidder"),
            primaryImageId,
            auction.Images.Count,
            orderedBids);
    }

    private async Task EnsureRequesterIsActiveAdminAsync(Guid requesterUserId, CancellationToken cancellationToken)
    {
        var requester = await _users.GetByIdAsync(requesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can manage auctions.");
        }
    }
}

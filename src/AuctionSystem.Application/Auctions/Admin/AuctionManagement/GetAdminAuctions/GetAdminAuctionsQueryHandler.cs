using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctions;

public sealed class GetAdminAuctionsQueryHandler : IRequestHandler<GetAdminAuctionsQuery, IReadOnlyList<AdminAuctionListItemDto>>
{
    private readonly IUserRepository _users;
    private readonly IAuctionRepository _auctions;

    public GetAdminAuctionsQueryHandler(IUserRepository users, IAuctionRepository auctions)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
    }

    public async Task<IReadOnlyList<AdminAuctionListItemDto>> Handle(GetAdminAuctionsQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        await EnsureRequesterIsActiveAdminAsync(request.RequesterUserId, cancellationToken);

        var auctions = await _auctions.ListAsync(_ => true, cancellationToken);
        var sellerIds = auctions.Select(x => x.SellerId).Distinct().ToArray();
        var sellers = await _users.ListAsync(user => sellerIds.Contains(user.Id), cancellationToken);
        var sellersById = sellers.ToDictionary(x => x.Id, x => x.FullName);

        return auctions
            .OrderBy(x => x.Status switch
            {
                AuctionStatus.Active => 0,
                AuctionStatus.Draft => 1,
                AuctionStatus.Ended => 2,
                _ => 3
            })
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminAuctionListItemDto(
                x.Id,
                x.Title,
                x.SellerId,
                sellersById.GetValueOrDefault(x.SellerId, "Unknown Seller"),
                x.Category,
                x.CurrentPrice.Amount,
                x.CurrentPrice.Currency,
                x.Bids.Count,
                x.EndTimeUtc,
                x.Status.ToString()))
            .ToList();
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

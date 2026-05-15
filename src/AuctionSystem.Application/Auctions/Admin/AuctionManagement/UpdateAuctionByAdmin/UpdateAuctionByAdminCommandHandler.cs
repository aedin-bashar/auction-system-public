using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using MediatR;

namespace AuctionSystem.Application.Auctions.Admin.AuctionManagement.UpdateAuctionByAdmin;

public sealed class UpdateAuctionByAdminCommandHandler : IRequestHandler<UpdateAuctionByAdminCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IAuctionRepository _auctions;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAuctionByAdminCommandHandler(
        IUserRepository users,
        IAuctionRepository auctions,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Unit> Handle(UpdateAuctionByAdminCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can manage auctions.");
        }

        var auction = await _auctions.GetWithBidsByIdAsync(request.AuctionId, cancellationToken);
        if (auction is null)
        {
            throw new KeyNotFoundException("Auction was not found.");
        }

        if (auction.Status == AuctionStatus.Ended)
        {
            throw new InvalidOperationException("Ended auctions cannot be edited.");
        }

        auction.UpdateDetails(
            request.Title,
            request.Category,
            request.Description,
            request.EndTimeUtc);

        var requestedPrice = Money.Create(request.StartingPriceAmount, request.Currency);
        var isPriceChanged = auction.StartingPrice.Amount != requestedPrice.Amount
            || !string.Equals(auction.StartingPrice.Currency, requestedPrice.Currency, StringComparison.Ordinal);

        if (isPriceChanged)
        {
            if (auction.Bids.Count > 0)
            {
                throw new InvalidOperationException("Starting price or currency cannot be changed after bids are placed.");
            }

            auction.UpdateStartingPrice(requestedPrice);
        }

        if (request.ReplaceImages)
        {
            var images = (request.Images ?? Array.Empty<UpdateAuctionImageInput>())
                .OrderBy(x => x.SortOrder)
                .Select(x => (x.FileName, x.ContentType, x.Content))
                .ToList();

            auction.ReplaceImages(images);
        }
        else if (request.Images is { Count: > 0 })
        {
            var startSortOrder = auction.Images.Count;
            foreach (var image in request.Images.OrderBy(x => x.SortOrder))
            {
                auction.AddImage(image.FileName, image.ContentType, image.Content, startSortOrder++);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

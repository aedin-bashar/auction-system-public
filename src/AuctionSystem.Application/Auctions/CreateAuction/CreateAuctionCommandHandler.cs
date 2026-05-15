using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using MediatR;

namespace AuctionSystem.Application.Auctions.CreateAuction;

public sealed class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly IAuctionRepository _auctions;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAuctionCommandHandler(
        IUserRepository users,
        IAuctionRepository auctions,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Guid> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var seller = await _users.GetByIdAsync(request.SellerId, cancellationToken);
        if (seller is null)
        {
            throw new KeyNotFoundException("Seller was not found.");
        }

        if (!seller.IsActive)
        {
            throw new InvalidOperationException("Inactive users cannot create auctions.");
        }

        if (seller.Role is not (UserRole.Seller or UserRole.Admin))
        {
            throw new InvalidOperationException("Only users with Seller or Admin role can create auctions.");
        }

        var startingPrice = Money.Create(request.StartingPriceAmount, request.Currency);

        var auction = Auction.Create(
            request.SellerId,
            request.Title.Trim(),
            startingPrice,
            request.EndTimeUtc,
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.Category);

        if (request.Images is { Count: > 0 })
        {
            foreach (var image in request.Images.OrderBy(x => x.SortOrder))
            {
                auction.AddImage(image.FileName, image.ContentType, image.Content, image.SortOrder);
            }
        }

        auction.Start();

        await _auctions.AddAsync(auction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auction.Id;
    }
}

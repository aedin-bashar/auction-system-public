using AuctionSystem.Application.Auctions.GetActiveAuctions;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.ValueObjects;
using Moq;

namespace AuctionSystem.UnitTests.Application.Auctions.GetActiveAuctions;

public class GetActiveAuctionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithCategoryAndPriceFilters_ReturnsMatchingItems()
    {
        var nowUtc = DateTime.UtcNow;

        var matching = CreateActiveAuction(
            title: "Gaming Laptop",
            category: "Electronics",
            description: "High-end category electronics",
            amount: 600m,
            endTimeUtc: nowUtc.AddHours(2));

        var outOfCategory = CreateActiveAuction(
            title: "Office Chair",
            category: "Furniture",
            description: "Furniture",
            amount: 550m,
            endTimeUtc: nowUtc.AddHours(1));

        var outOfPrice = CreateActiveAuction(
            title: "Laptop Sleeve",
            category: "Electronics",
            description: "Laptop accessory",
            amount: 50m,
            endTimeUtc: nowUtc.AddHours(3));

        var endedAuction = CreateEndedAuction(
            title: "Gaming Laptop Pro",
            category: "Electronics",
            description: "Category electronics",
            amount: 650m,
            endTimeUtc: nowUtc.AddHours(2));

        var seeded = new List<Auction> { matching, outOfCategory, outOfPrice, endedAuction };

        var auctions = new Mock<IAuctionRepository>();
        auctions
            .Setup(x => x.ListActiveAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? category, decimal? minPrice, decimal? maxPrice, int pageNumber, int pageSize, CancellationToken _) =>
                ApplyActiveAuctionQuery(seeded, nowUtc, category, minPrice, maxPrice, pageNumber, pageSize));

        var handler = new GetActiveAuctionsQueryHandler(auctions.Object);
        var query = new GetActiveAuctionsQuery(
            Category: "  Electronics  ",
            MinPrice: 100m,
            MaxPrice: 700m,
            PageNumber: 1,
            PageSize: 10);

        var result = await handler.Handle(query, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(matching.Id, item.Id);
        Assert.Equal(matching.SellerId, item.SellerId);
        Assert.Equal("Gaming Laptop", item.Title);
        Assert.Equal("Electronics", item.Category);
        Assert.Equal(600m, item.PriceAmount);
        Assert.Equal("USD", item.Currency);

        auctions.Verify(x => x.ListActiveAsync(
            It.IsAny<string?>(),
            It.IsAny<decimal?>(),
            It.IsAny<decimal?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var auctions = new Mock<IAuctionRepository>();
        var handler = new GetActiveAuctionsQueryHandler(auctions.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!, CancellationToken.None));

        auctions.Verify(x => x.ListActiveAsync(
            It.IsAny<string?>(),
            It.IsAny<decimal?>(),
            It.IsAny<decimal?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAuctionHasBids_UsesCurrentPriceForFilteringAndResponse()
    {
        var nowUtc = DateTime.UtcNow;

        var auction = CreateActiveAuction(
            title: "Premium Console",
            category: "Gaming",
            description: "Limited edition",
            amount: 1000m,
            endTimeUtc: nowUtc.AddHours(2));

        var bidderId = Guid.NewGuid();
        auction.PlaceBid(bidderId, Money.Create(1250m, "USD"), nowUtc.AddMinutes(-1));

        var seeded = new List<Auction> { auction };

        var auctions = new Mock<IAuctionRepository>();
        auctions
            .Setup(x => x.ListActiveAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? category, decimal? minPrice, decimal? maxPrice, int pageNumber, int pageSize, CancellationToken _) =>
                ApplyActiveAuctionQuery(seeded, nowUtc, category, minPrice, maxPrice, pageNumber, pageSize));

        var handler = new GetActiveAuctionsQueryHandler(auctions.Object);
        var query = new GetActiveAuctionsQuery(
            Category: "Gaming",
            MinPrice: 1200m,
            MaxPrice: 1300m,
            PageNumber: 1,
            PageSize: 10);

        var result = await handler.Handle(query, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(1250m, item.PriceAmount);
        Assert.Equal("USD", item.Currency);
    }

    private static IReadOnlyList<Auction> ApplyActiveAuctionQuery(
        IEnumerable<Auction> seeded,
        DateTime nowUtc,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        int pageNumber,
        int pageSize)
    {
        var query = seeded
            .Where(auction => auction.Status == AuctionStatus.Active && auction.EndTimeUtc > nowUtc);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(auction =>
                string.Equals(auction.Category, category.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(auction => auction.CurrentPrice.Amount >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(auction => auction.CurrentPrice.Amount <= maxPrice.Value);
        }

        return query
            .OrderBy(auction => auction.EndTimeUtc)
            .ThenBy(auction => auction.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    private static Auction CreateActiveAuction(string title, string category, string? description, decimal amount, DateTime endTimeUtc)
    {
        var auction = Auction.Create(
            sellerId: Guid.NewGuid(),
            title: title,
            startingPrice: Money.Create(amount, "USD"),
            endTimeUtc: endTimeUtc,
            description: description,
            category: category,
            nowUtc: DateTime.UtcNow.AddMinutes(-5));

        auction.Start(DateTime.UtcNow.AddMinutes(-4));
        return auction;
    }

    private static Auction CreateEndedAuction(string title, string category, string? description, decimal amount, DateTime endTimeUtc)
    {
        var auction = Auction.Create(
            sellerId: Guid.NewGuid(),
            title: title,
            startingPrice: Money.Create(amount, "USD"),
            endTimeUtc: endTimeUtc,
            description: description,
            category: category,
            nowUtc: DateTime.UtcNow.AddMinutes(-10));

        auction.Start(DateTime.UtcNow.AddMinutes(-9));
        auction.End(DateTime.UtcNow.AddMinutes(-8));
        return auction;
    }
}

using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactionDetail;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Payments.Admin.TransactionManagement;

public class GetAdminTransactionDetailQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsTransactionDetail()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var transaction = new AdminTransactionDetailDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Seller User",
            "Payment",
            125m,
            "USD",
            "Completed",
            "PAY-123",
            "Auction payout",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1),
            null,
            null,
            null,
            400m,
            "USD");

        var query = new GetAdminTransactionDetailQuery(admin.Id, transaction.TransactionId);

        var users = new Mock<IUserRepository>();
        var transactionStore = new Mock<IAdminTransactionStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        transactionStore.Setup(x => x.GetByIdAsync(transaction.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);

        var handler = new GetAdminTransactionDetailQueryHandler(users.Object, transactionStore.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(transaction, result);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var query = new GetAdminTransactionDetailQuery(bidder.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var transactionStore = new Mock<IAdminTransactionStore>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bidder);

        var handler = new GetAdminTransactionDetailQueryHandler(users.Object, transactionStore.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));

        transactionStore.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTransactionNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var query = new GetAdminTransactionDetailQuery(admin.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var transactionStore = new Mock<IAdminTransactionStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        transactionStore.Setup(x => x.GetByIdAsync(query.TransactionId, It.IsAny<CancellationToken>())).ReturnsAsync((AdminTransactionDetailDto?)null);

        var handler = new GetAdminTransactionDetailQueryHandler(users.Object, transactionStore.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}
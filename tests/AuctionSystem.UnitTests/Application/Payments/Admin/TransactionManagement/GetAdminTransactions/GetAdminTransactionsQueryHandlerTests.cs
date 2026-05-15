using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactions;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Payments.Admin.TransactionManagement;

public class GetAdminTransactionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAdmin_ReturnsTransactions()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var query = new GetAdminTransactionsQuery(admin.Id);
        IReadOnlyList<AdminTransactionListItemDto> transactions =
        [
            new AdminTransactionListItemDto(Guid.NewGuid(), Guid.NewGuid(), "Seller User", "Payment", 125m, "USD", "Completed", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1))
        ];

        var users = new Mock<IUserRepository>();
        var transactionStore = new Mock<IAdminTransactionStore>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        transactionStore.Setup(x => x.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactions);

        var handler = new GetAdminTransactionsQueryHandler(users.Object, transactionStore.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(transactions, result);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var query = new GetAdminTransactionsQuery(bidder.Id);

        var users = new Mock<IUserRepository>();
        var transactionStore = new Mock<IAdminTransactionStore>();

        users.Setup(x => x.GetByIdAsync(bidder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bidder);

        var handler = new GetAdminTransactionsQueryHandler(users.Object, transactionStore.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));

        transactionStore.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
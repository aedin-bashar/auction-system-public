using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Payments.Admin.TransactionManagement;

public class ProcessRefundCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidAdminAndTransaction_ProcessesRefundAndReturnsUpdatedWalletBalance()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var transactionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var initialTransaction = new AdminTransactionDetailDto(
            transactionId,
            Guid.NewGuid(),
            "Bidder One",
            "Bid Payment",
            120.00m,
            "USD",
            "Completed",
            "AUC-TEST / BID-TEST",
            "Bid payment",
            now.AddHours(-2),
            now.AddHours(-2),
            null,
            null,
            null,
            400.00m,
            "USD");

        var command = new ProcessRefundCommand(admin.Id, transactionId, "  Duplicate charge  ");

        var users = new Mock<IUserRepository>();
        var transactions = new InMemoryAdminTransactionStore(initialTransaction);
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new ProcessRefundCommandHandler(users.Object, transactions, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);
        var stored = await transactions.GetByIdAsync(transactionId, CancellationToken.None);

        Assert.Equal(command.TransactionId, result.TransactionId);
        Assert.Equal("Refunded", result.Status);
        Assert.Equal(520.00m, result.WalletBalanceAmount);
        Assert.Equal("USD", result.WalletBalanceCurrency);
        Assert.Equal("Duplicate charge", result.RefundReason);
        Assert.NotNull(result.RefundedAtUtc);

        Assert.NotNull(stored);
        Assert.Equal("Refunded", stored!.Status);
        Assert.Equal(520.00m, stored.WalletBalanceAmount);

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequesterIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        var bidder = User.Register("bidder@example.com", "Bidder User", UserRole.Bidder);
        var command = new ProcessRefundCommand(bidder.Id, Guid.NewGuid(), "Any reason");

        var users = new Mock<IUserRepository>();
        var transactions = new InMemoryAdminTransactionStore();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(command.RequesterUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bidder);

        var handler = new ProcessRefundCommandHandler(users.Object, transactions, unitOfWork.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTransactionNotFound_ThrowsKeyNotFoundException()
    {
        var admin = User.Register("admin@example.com", "Admin User", UserRole.Admin);
        var command = new ProcessRefundCommand(admin.Id, Guid.NewGuid(), "Invalid charge");

        var users = new Mock<IUserRepository>();
        var transactions = new InMemoryAdminTransactionStore();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var handler = new ProcessRefundCommandHandler(users.Object, transactions, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class InMemoryAdminTransactionStore : IAdminTransactionStore
    {
        private readonly Dictionary<Guid, AdminTransactionDetailDto> _transactions = new();

        public InMemoryAdminTransactionStore(params AdminTransactionDetailDto[] seed)
        {
            foreach (var item in seed)
            {
                _transactions[item.TransactionId] = item;
            }
        }

        public Task<IReadOnlyList<AdminTransactionListItemDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AdminTransactionListItemDto> items = _transactions.Values
                .Select(x => new AdminTransactionListItemDto(
                    x.TransactionId,
                    x.UserId,
                    x.UserName,
                    x.Type,
                    x.Amount,
                    x.Currency,
                    x.Status,
                    x.CreatedAtUtc,
                    x.UpdatedAtUtc))
                .ToList();

            return Task.FromResult(items);
        }

        public Task<AdminTransactionDetailDto?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        {
            _transactions.TryGetValue(transactionId, out var transaction);
            return Task.FromResult(transaction);
        }

        public Task<AdminTransactionDetailDto?> ProcessRefundAsync(ProcessAdminRefundRequest request, CancellationToken cancellationToken = default)
        {
            if (!_transactions.TryGetValue(request.TransactionId, out var transaction))
            {
                return Task.FromResult<AdminTransactionDetailDto?>(null);
            }

            var updatedBalance = (transaction.WalletBalanceAmount ?? 0m) + transaction.Amount;
            var updated = transaction with
            {
                Status = "Refunded",
                UpdatedAtUtc = request.RequestedAtUtc,
                RefundedAtUtc = request.RequestedAtUtc,
                RefundedBy = "Admin User",
                RefundReason = request.Reason,
                WalletBalanceAmount = updatedBalance,
                WalletBalanceCurrency = transaction.Currency
            };

            _transactions[request.TransactionId] = updated;
            return Task.FromResult<AdminTransactionDetailDto?>(updated);
        }
    }
}

using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Repositories;

public sealed class AdminTransactionStore : IAdminTransactionStore
{
    private readonly ApplicationDbContext _db;

    public AdminTransactionStore(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<AdminTransactionListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var bids = await _db.Bids
            .AsNoTracking()
            .OrderByDescending(x => x.PlacedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.BidderId,
                Amount = x.Amount.Amount,
                Currency = x.Amount.Currency,
                x.PlacedAtUtc
            })
            .ToListAsync(cancellationToken);

        if (bids.Count == 0)
        {
            return Array.Empty<AdminTransactionListItemDto>();
        }

        var userIds = bids.Select(x => x.BidderId).Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);

        var transactionIds = bids.Select(x => x.Id).ToList();
        var refunds = await _db.AdminTransactionRefunds
            .AsNoTracking()
            .Where(x => transactionIds.Contains(x.TransactionId))
            .Select(x => new { x.TransactionId, x.UpdatedAtUtc })
            .ToDictionaryAsync(x => x.TransactionId, x => x.UpdatedAtUtc, cancellationToken);

        var transactions = bids
            .Select(x =>
            {
                var refundedAt = refunds.TryGetValue(x.Id, out var updatedAt) ? updatedAt : (DateTime?)null;
                return new AdminTransactionListItemDto(
                    x.Id,
                    x.BidderId,
                    users.TryGetValue(x.BidderId, out var fullName) ? fullName : "Unknown User",
                    "Bid Payment",
                    x.Amount,
                    x.Currency,
                    refundedAt.HasValue ? "Refunded" : "Completed",
                    x.PlacedAtUtc,
                    refundedAt ?? x.PlacedAtUtc);
            })
            .ToList();

        return transactions;
    }

    public async Task<AdminTransactionDetailDto?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        if (transactionId == Guid.Empty) throw new ArgumentException("Transaction id is required.", nameof(transactionId));

        var row = await (
            from bid in _db.Bids.AsNoTracking()
            where bid.Id == transactionId
            join auction in _db.Auctions.AsNoTracking() on bid.AuctionId equals auction.Id
            join user in _db.Users.AsNoTracking() on bid.BidderId equals user.Id
            join refund in _db.AdminTransactionRefunds.AsNoTracking() on bid.Id equals refund.TransactionId into refundJoin
            from refund in refundJoin.DefaultIfEmpty()
            select new
            {
                Bid = bid,
                AuctionTitle = auction.Title,
                User = user,
                Refund = refund
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var refundedBy = row.Refund is null
            ? null
            : await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == row.Refund.RefundedByUserId)
                .Select(x => x.FullName)
                .SingleOrDefaultAsync(cancellationToken);

        var walletBalance = await CalculateWalletBalanceAsync(row.User.Id, row.Bid.Id, row.Refund is not null, cancellationToken);

        return new AdminTransactionDetailDto(
            row.Bid.Id,
            row.User.Id,
            row.User.FullName,
            "Bid Payment",
            row.Bid.Amount.Amount,
            row.Bid.Amount.Currency,
            row.Refund is null ? "Completed" : "Refunded",
            $"AUC-{row.Bid.AuctionId:N}".Substring(0, 12) + $" / BID-{row.Bid.Id:N}".Substring(0, 12),
            $"Bid payment for auction \"{row.AuctionTitle}\"",
            row.Bid.PlacedAtUtc,
            row.Refund is null ? row.Bid.PlacedAtUtc : row.Refund.UpdatedAtUtc,
            row.Refund?.RefundedAtUtc,
            refundedBy,
            row.Refund?.Reason,
            walletBalance,
            row.Bid.Amount.Currency);
    }

    public async Task<AdminTransactionDetailDto?> ProcessRefundAsync(ProcessAdminRefundRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (request.TransactionId == Guid.Empty) throw new ArgumentException("Transaction id is required.", nameof(request));
        if (request.RefundedByUserId == Guid.Empty) throw new ArgumentException("RefundedBy user id is required.", nameof(request));

        var bid = await _db.Bids
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.TransactionId, cancellationToken);

        if (bid is null)
        {
            return null;
        }

        var now = request.RequestedAtUtc.Kind == DateTimeKind.Utc
            ? request.RequestedAtUtc
            : request.RequestedAtUtc.ToUniversalTime();

        var refund = await _db.AdminTransactionRefunds
            .SingleOrDefaultAsync(x => x.TransactionId == request.TransactionId, cancellationToken);

        if (refund is null)
        {
            refund = new AdminTransactionRefund
            {
                TransactionId = request.TransactionId,
                RefundedByUserId = request.RefundedByUserId,
                Reason = request.Reason,
                RefundedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _db.AdminTransactionRefunds.AddAsync(refund, cancellationToken);
        }
        else
        {
            refund.RefundedByUserId = request.RefundedByUserId;
            refund.Reason = request.Reason;
            refund.RefundedAtUtc = now;
            refund.UpdatedAtUtc = now;
        }

        return await GetByIdCoreAsync(
            request.TransactionId,
            forceRefunded: true,
            overrideRefundedByUserId: request.RefundedByUserId,
            overrideRefundReason: request.Reason,
            overrideRefundedAtUtc: now,
            cancellationToken);
    }

    private async Task<AdminTransactionDetailDto?> GetByIdCoreAsync(
        Guid transactionId,
        bool forceRefunded,
        Guid? overrideRefundedByUserId,
        string? overrideRefundReason,
        DateTime? overrideRefundedAtUtc,
        CancellationToken cancellationToken)
    {
        var row = await (
            from bid in _db.Bids.AsNoTracking()
            where bid.Id == transactionId
            join auction in _db.Auctions.AsNoTracking() on bid.AuctionId equals auction.Id
            join user in _db.Users.AsNoTracking() on bid.BidderId equals user.Id
            join refund in _db.AdminTransactionRefunds.AsNoTracking() on bid.Id equals refund.TransactionId into refundJoin
            from refund in refundJoin.DefaultIfEmpty()
            select new
            {
                Bid = bid,
                AuctionTitle = auction.Title,
                User = user,
                Refund = refund
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var isRefunded = forceRefunded || row.Refund is not null;
        var refundedByUserId = overrideRefundedByUserId ?? row.Refund?.RefundedByUserId;
        var refundedBy = refundedByUserId.HasValue
            ? await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == refundedByUserId.Value)
                .Select(x => x.FullName)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        var refundReason = overrideRefundReason ?? row.Refund?.Reason;
        var refundedAt = overrideRefundedAtUtc ?? row.Refund?.RefundedAtUtc;
        var updatedAt = overrideRefundedAtUtc ?? row.Refund?.UpdatedAtUtc ?? row.Bid.PlacedAtUtc;
        var walletBalance = await CalculateWalletBalanceAsync(row.User.Id, row.Bid.Id, isRefunded, cancellationToken);

        return new AdminTransactionDetailDto(
            row.Bid.Id,
            row.User.Id,
            row.User.FullName,
            "Bid Payment",
            row.Bid.Amount.Amount,
            row.Bid.Amount.Currency,
            isRefunded ? "Refunded" : "Completed",
            $"AUC-{row.Bid.AuctionId:N}".Substring(0, 12) + $" / BID-{row.Bid.Id:N}".Substring(0, 12),
            $"Bid payment for auction \"{row.AuctionTitle}\"",
            row.Bid.PlacedAtUtc,
            updatedAt,
            refundedAt,
            refundedBy,
            refundReason,
            walletBalance,
            row.Bid.Amount.Currency);
    }

    private async Task<decimal> CalculateWalletBalanceAsync(
        Guid userId,
        Guid transactionIdOverride,
        bool refundedOverride,
        CancellationToken cancellationToken)
    {
        var transactionAmount = await _db.Bids
            .AsNoTracking()
            .Where(x => x.BidderId == userId && x.Id == transactionIdOverride)
            .Select(x => (decimal?)x.Amount.Amount)
            .SingleOrDefaultAsync(cancellationToken);

        if (!transactionAmount.HasValue)
        {
            return 0m;
        }

        var completedSum = await (
            from bid in _db.Bids.AsNoTracking()
            where bid.BidderId == userId
                  && bid.Id != transactionIdOverride
            join refund in _db.AdminTransactionRefunds.AsNoTracking() on bid.Id equals refund.TransactionId into refundJoin
            from refund in refundJoin.DefaultIfEmpty()
            where refund == null
            select (decimal?)bid.Amount.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var refundedSum = await (
            from bid in _db.Bids.AsNoTracking()
            where bid.BidderId == userId
                  && bid.Id != transactionIdOverride
            join refund in _db.AdminTransactionRefunds.AsNoTracking() on bid.Id equals refund.TransactionId
            select (decimal?)bid.Amount.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var balance = refundedSum - completedSum + (refundedOverride ? transactionAmount.Value : -transactionAmount.Value);

        return decimal.Round(balance, 2);
    }
}

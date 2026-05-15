using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Repositories;

public sealed class PaymentMethodStore : IPaymentMethodStore
{
    private readonly ApplicationDbContext _db;

    public PaymentMethodStore(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<PaymentMethodDto> AddAsync(AddPaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var now = DateTime.UtcNow;

        if (request.IsDefault)
        {
            var currentDefaults = await _db.PaymentMethods
                .Where(x => x.UserId == request.UserId && x.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var currentDefault in currentDefaults)
            {
                currentDefault.SetDefault(false, now);
            }
        }

        var entity = new PaymentMethod(
            Guid.NewGuid(),
            request.UserId,
            request.Type,
            request.Provider,
            request.Last4,
            request.ExpiryMonth,
            request.ExpiryYear,
            request.HolderName,
            request.IsDefault,
            now);

        await _db.PaymentMethods.AddAsync(entity, cancellationToken);

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<PaymentMethodDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var methods = await _db.PaymentMethods
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return methods
            .Select(ToDto)
            .ToList();
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentMethods
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == paymentMethodId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _db.PaymentMethods.Remove(entity);
        return true;
    }

    private static PaymentMethodDto ToDto(PaymentMethod method)
    {
        return new PaymentMethodDto(
            method.Id,
            method.UserId,
            method.Type,
            method.Provider,
            method.Last4,
            method.ExpiryMonth,
            method.ExpiryYear,
            method.HolderName,
            method.IsDefault,
            method.CreatedAtUtc,
            method.UpdatedAtUtc);
    }
}
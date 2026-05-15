using System.Linq.Expressions;
using AuctionSystem.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Repositories;

public class EfRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class
{
    protected readonly DbContext Db;

    public EfRepository(DbContext db)
    {
        Db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public virtual async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        // Works for single-key aggregates; aligns with current Domain usage (Guid keys).
        return await Db.Set<TAggregate>().FindAsync([id!], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TAggregate>> ListAsync(
        Expression<Func<TAggregate, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));

        return await Db.Set<TAggregate>()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(TAggregate entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        await Db.Set<TAggregate>().AddAsync(entity, cancellationToken);
    }

    public virtual void Update(TAggregate entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        Db.Set<TAggregate>().Update(entity);
    }

    public virtual void Remove(TAggregate entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        Db.Set<TAggregate>().Remove(entity);
    }
}
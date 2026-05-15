using System.Linq.Expressions;

namespace AuctionSystem.Domain.Abstractions;

/// <summary>
/// Generic repository contract for Aggregate Roots (DDD).
/// Implementations belong to Infrastructure.
/// </summary>
public interface IRepository<TAggregate, TId>
    where TAggregate : class
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TAggregate>> ListAsync(
        Expression<Func<TAggregate, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task AddAsync(TAggregate entity, CancellationToken cancellationToken = default);

    void Update(TAggregate entity);

    void Remove(TAggregate entity);
}
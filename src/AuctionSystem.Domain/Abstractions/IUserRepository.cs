using AuctionSystem.Domain.Users;

namespace AuctionSystem.Domain.Abstractions;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
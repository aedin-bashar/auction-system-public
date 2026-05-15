using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Repositories;

public sealed class UserRepository : EfRepository<User, Guid>, IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalized = email.Trim().ToLowerInvariant();

        return _db.Users
            .SingleOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public override Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        return base.AddAsync(entity, cancellationToken);
    }
}
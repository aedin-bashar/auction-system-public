using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Security;

public sealed class PasswordStore : IPasswordStore
{
    private readonly ApplicationDbContext _db;

    public PasswordStore(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task SetPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.", nameof(password));

        var hashResult = PasswordHashing.Hash(password);
        var nowUtc = DateTime.UtcNow;

        var existing = await _db.UserPasswords
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (existing is null)
        {
            var record = new UserPassword(userId, hashResult.Hash, hashResult.Salt, hashResult.Iterations, nowUtc);
            await _db.UserPasswords.AddAsync(record, cancellationToken);
        }
        else
        {
            existing.Update(hashResult.Hash, hashResult.Salt, hashResult.Iterations, nowUtc);
        }

    }
}

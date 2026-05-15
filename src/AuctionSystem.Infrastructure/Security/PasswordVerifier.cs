using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Security;

public sealed class PasswordVerifier : IPasswordVerifier
{
    private readonly ApplicationDbContext _db;

    public PasswordVerifier(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<bool> VerifyAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var stored = await _db.UserPasswords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (stored is null)
        {
            return false;
        }

        return PasswordHashing.Verify(password, stored.Salt, stored.PasswordHash, stored.Iterations);
    }
}

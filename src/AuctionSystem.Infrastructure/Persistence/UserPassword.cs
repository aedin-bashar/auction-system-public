namespace AuctionSystem.Infrastructure.Persistence;

public sealed class UserPassword
{
    private UserPassword() { }

    public UserPassword(Guid userId, byte[] passwordHash, byte[] salt, int iterations, DateTime createdAtUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (passwordHash is null || passwordHash.Length == 0)
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }
        if (salt is null || salt.Length == 0)
        {
            throw new ArgumentException("Password salt is required.", nameof(salt));
        }
        if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations));

        UserId = userId;
        PasswordHash = passwordHash;
        Salt = salt;
        Iterations = iterations;
        CreatedAtUtc = EnsureUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid UserId { get; private set; }
    public byte[] PasswordHash { get; private set; } = Array.Empty<byte>();
    public byte[] Salt { get; private set; } = Array.Empty<byte>();
    public int Iterations { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(byte[] passwordHash, byte[] salt, int iterations, DateTime updatedAtUtc)
    {
        if (passwordHash is null || passwordHash.Length == 0)
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }
        if (salt is null || salt.Length == 0)
        {
            throw new ArgumentException("Password salt is required.", nameof(salt));
        }
        if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations));

        PasswordHash = passwordHash;
        Salt = salt;
        Iterations = iterations;
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        return value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}

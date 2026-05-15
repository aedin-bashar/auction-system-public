using System.Security.Cryptography;

namespace AuctionSystem.Infrastructure.Security;

public static class PasswordHashing
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 150_000;

    public static PasswordHashResult Hash(string password, int? iterations = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var iterationCount = iterations ?? DefaultIterations;

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterationCount,
            HashAlgorithmName.SHA256,
            HashSize);

        return new PasswordHashResult(hash, salt, iterationCount);
    }

    public static bool Verify(string password, byte[] salt, byte[] expectedHash, int iterations)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (salt is null || salt.Length == 0 || expectedHash is null || expectedHash.Length == 0 || iterations <= 0)
        {
            return false;
        }

        var computed = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(computed, expectedHash);
    }
}

public sealed record PasswordHashResult(byte[] Hash, byte[] Salt, int Iterations);

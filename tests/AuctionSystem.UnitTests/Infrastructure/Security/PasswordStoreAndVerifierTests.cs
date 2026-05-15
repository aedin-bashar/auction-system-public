using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.UnitTests.Infrastructure.Security;

public class PasswordStoreAndVerifierTests
{
    [Fact]
    public async Task SetPasswordAsync_WhenNoExistingRecord_AddsUserPassword()
    {
        await using var db = CreateDbContext();
        var store = new PasswordStore(db);
        var userId = Guid.NewGuid();

        await store.SetPasswordAsync(userId, "Secret123!", CancellationToken.None);
        await db.SaveChangesAsync();

        var stored = await db.UserPasswords.SingleAsync(x => x.UserId == userId);

        Assert.NotEmpty(stored.PasswordHash);
        Assert.NotEmpty(stored.Salt);
        Assert.True(stored.Iterations > 0);
        Assert.True(PasswordHashing.Verify("Secret123!", stored.Salt, stored.PasswordHash, stored.Iterations));
    }

    [Fact]
    public async Task SetPasswordAsync_WhenRecordExists_UpdatesStoredPassword()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var initial = PasswordHashing.Hash("OldSecret123!");
        var createdAt = DateTime.UtcNow.AddDays(-1);
        db.UserPasswords.Add(new UserPassword(userId, initial.Hash, initial.Salt, initial.Iterations, createdAt));
        await db.SaveChangesAsync();

        var store = new PasswordStore(db);

        await store.SetPasswordAsync(userId, "NewSecret123!", CancellationToken.None);
        await db.SaveChangesAsync();

        var stored = await db.UserPasswords.SingleAsync(x => x.UserId == userId);

        Assert.True(PasswordHashing.Verify("NewSecret123!", stored.Salt, stored.PasswordHash, stored.Iterations));
        Assert.False(PasswordHashing.Verify("OldSecret123!", stored.Salt, stored.PasswordHash, stored.Iterations));
        Assert.Equal(createdAt, stored.CreatedAtUtc);
        Assert.True(stored.UpdatedAtUtc >= createdAt);
    }

    [Fact]
    public async Task VerifyAsync_WithMatchingPassword_ReturnsTrue()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var hash = PasswordHashing.Hash("Secret123!");
        db.UserPasswords.Add(new UserPassword(userId, hash.Hash, hash.Salt, hash.Iterations, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var verifier = new PasswordVerifier(db);

        var isValid = await verifier.VerifyAsync(userId, "Secret123!", CancellationToken.None);

        Assert.True(isValid);
    }

    [Fact]
    public async Task VerifyAsync_WithMissingRecordOrInvalidInput_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var verifier = new PasswordVerifier(db);

        Assert.False(await verifier.VerifyAsync(Guid.Empty, "Secret123!", CancellationToken.None));
        Assert.False(await verifier.VerifyAsync(Guid.NewGuid(), "", CancellationToken.None));
        Assert.False(await verifier.VerifyAsync(Guid.NewGuid(), "Secret123!", CancellationToken.None));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AuctionSystem-PasswordTests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
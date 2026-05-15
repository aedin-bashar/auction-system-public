using AuctionSystem.Infrastructure.Security;

namespace AuctionSystem.UnitTests.Infrastructure.Security;

public class PasswordHashingTests
{
    [Fact]
    public void Hash_WithValidPassword_ReturnsHashSaltAndIterationsThatVerify()
    {
        var result = PasswordHashing.Hash("Secret123!", iterations: 200_000);

        Assert.NotEmpty(result.Hash);
        Assert.NotEmpty(result.Salt);
        Assert.Equal(200_000, result.Iterations);
        Assert.True(PasswordHashing.Verify("Secret123!", result.Salt, result.Hash, result.Iterations));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var result = PasswordHashing.Hash("Secret123!");

        var isValid = PasswordHashing.Verify("Wrong123!", result.Salt, result.Hash, result.Iterations);

        Assert.False(isValid);
    }

    [Fact]
    public void Verify_WithInvalidArguments_ReturnsFalse()
    {
        Assert.False(PasswordHashing.Verify("", new byte[] { 1 }, new byte[] { 2 }, 10));
        Assert.False(PasswordHashing.Verify("Secret123!", Array.Empty<byte>(), new byte[] { 2 }, 10));
        Assert.False(PasswordHashing.Verify("Secret123!", new byte[] { 1 }, Array.Empty<byte>(), 10));
        Assert.False(PasswordHashing.Verify("Secret123!", new byte[] { 1 }, new byte[] { 2 }, 0));
    }

    [Fact]
    public void Hash_WithEmptyPassword_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PasswordHashing.Hash(""));
    }
}
using AuctionSystem.Domain.Users;

namespace AuctionSystem.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Register_WithValidData_NormalizesAndSetsDefaults()
    {
        var now = new DateTime(2030, 01, 02, 03, 04, 05, DateTimeKind.Utc);

        var user = User.Register("  John.Doe@Example.com  ", "  John Doe  ", nowUtc: now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("john.doe@example.com", user.Email);
        Assert.Equal("John Doe", user.FullName);
        Assert.Null(user.PhoneNumber);
        Assert.Equal(UserRole.Bidder, user.Role);
        Assert.True(user.IsActive);
        Assert.Equal(now, user.CreatedAtUtc);
        Assert.Equal(now, user.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("a@")]
    public void Register_WithInvalidEmail_Throws(string email)
    {
        Assert.Throws<ArgumentException>(() => User.Register(email, "John Doe"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")] // too short
    public void Register_WithInvalidFullName_Throws(string fullName)
    {
        Assert.ThrowsAny<Exception>(() => User.Register("john@example.com", fullName));
    }

    [Fact]
    public void UpdateProfile_UpdatesNamePhoneAndTimestamp()
    {
        var user = User.Register("john@example.com", "John Doe", nowUtc: new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc));
        var now2 = new DateTime(2030, 02, 01, 0, 0, 0, DateTimeKind.Utc);

        user.UpdateProfile("Johnny Doe", "+1 (555) 123-4567", nowUtc: now2);

        Assert.Equal("Johnny Doe", user.FullName);
        Assert.Equal("+1 (555) 123-4567", user.PhoneNumber);
        Assert.Equal(now2, user.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeEmail_WithSameEmail_Throws()
    {
        var user = User.Register("john@example.com", "John Doe");

        Assert.Throws<InvalidOperationException>(() => user.ChangeEmail("  JOHN@EXAMPLE.COM "));
    }

    [Fact]
    public void Deactivate_Twice_Throws()
    {
        var user = User.Register("john@example.com", "John Doe");

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.Throws<InvalidOperationException>(() => user.Deactivate());
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        var user = User.Register("john@example.com", "John Doe");

        Assert.Throws<InvalidOperationException>(() => user.Activate());
    }

    [Fact]
    public void ChangeRole_ToDifferentRole_UpdatesRole()
    {
        var now = new DateTime(2031, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var user = User.Register("john@example.com", "John Doe", role: UserRole.Bidder, nowUtc: now);

        var now2 = new DateTime(2031, 01, 02, 0, 0, 0, DateTimeKind.Utc);
        user.ChangeRole(UserRole.Seller, nowUtc: now2);

        Assert.Equal(UserRole.Seller, user.Role);
        Assert.Equal(now2, user.UpdatedAtUtc);
    }
}
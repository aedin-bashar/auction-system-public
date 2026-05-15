using System.Net.Mail;
using System.Text.RegularExpressions;

namespace AuctionSystem.Domain.Users;

public enum UserRole
{
    Admin = 1,
    Seller = 2,
    Bidder = 3
}

public sealed class User
{
    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s()]{7,20}$", RegexOptions.Compiled);

    private User(Guid id, string email, string fullName, string? phoneNumber, UserRole role, DateTime nowUtc)
    {
        Id = id;
        Email = email;
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Role = role;
        IsActive = true;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    // For ORM materialization (kept private to preserve invariants)
    private User() { }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static User Register(
        string email,
        string fullName,
        UserRole role = UserRole.Bidder,
        string? phoneNumber = null,
        DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);
        var normalizedEmail = NormalizeAndValidateEmail(email);
        var normalizedName = NormalizeAndValidateFullName(fullName);
        var normalizedPhone = NormalizeAndValidatePhone(phoneNumber);
        EnsureValidRole(role);

        return new User(Guid.NewGuid(), normalizedEmail, normalizedName, normalizedPhone, role, now);
    }

    public void UpdateProfile(string fullName, string? phoneNumber = null, DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);
        FullName = NormalizeAndValidateFullName(fullName);
        PhoneNumber = NormalizeAndValidatePhone(phoneNumber);
        UpdatedAtUtc = now;
    }

    public void ChangeEmail(string newEmail, DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);
        var normalizedEmail = NormalizeAndValidateEmail(newEmail);

        if (string.Equals(Email, normalizedEmail, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("New email must be different from the current email.");
        }

        Email = normalizedEmail;
        UpdatedAtUtc = now;
    }

    public void ChangeRole(UserRole newRole, DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);
        EnsureValidRole(newRole);

        if (Role == newRole)
        {
            throw new InvalidOperationException("New role must be different from the current role.");
        }

        Role = newRole;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);

        if (!IsActive)
        {
            throw new InvalidOperationException("User is already deactivated.");
        }

        IsActive = false;
        UpdatedAtUtc = now;
    }

    public void Activate(DateTime? nowUtc = null)
    {
        var now = EnsureUtcOrDefault(nowUtc);

        if (IsActive)
        {
            throw new InvalidOperationException("User is already active.");
        }

        IsActive = true;
        UpdatedAtUtc = now;
    }

    private static void EnsureValidRole(UserRole role)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), "Invalid user role.");
        }
    }

    private static string NormalizeAndValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var trimmed = email.Trim();

        try
        {
            var addr = new MailAddress(trimmed);
            if (!string.Equals(addr.Address, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Email is invalid.", nameof(email));
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("Email is invalid.", nameof(email));
        }

        return trimmed.ToLowerInvariant();
    }

    private static string NormalizeAndValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        var normalized = fullName.Trim();

        if (normalized.Length < 2 || normalized.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(fullName), "Full name must be between 2 and 100 characters.");
        }

        return normalized;
    }

    private static string? NormalizeAndValidatePhone(string? phoneNumber)
    {
        if (phoneNumber is null)
        {
            return null;
        }

        var normalized = phoneNumber.Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        if (!PhoneRegex.IsMatch(normalized))
        {
            throw new ArgumentException("Phone number format is invalid.", nameof(phoneNumber));
        }

        return normalized;
    }

    private static DateTime EnsureUtcOrDefault(DateTime? nowUtc)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (now.Kind == DateTimeKind.Local)
        {
            now = now.ToUniversalTime();
        }
        else if (now.Kind == DateTimeKind.Unspecified)
        {
            now = DateTime.SpecifyKind(now, DateTimeKind.Utc);
        }

        return now;
    }
}
namespace AuctionSystem.Infrastructure.Persistence;

public sealed class PaymentMethod
{
    private PaymentMethod() { }

    public PaymentMethod(
        Guid id,
        Guid userId,
        string type,
        string provider,
        string last4,
        int expiryMonth,
        int expiryYear,
        string? holderName,
        bool isDefault,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Payment method id is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(last4) || last4.Trim().Length != 4)
        {
            throw new ArgumentException("Last4 is required and must be 4 digits.", nameof(last4));
        }

        Id = id;
        UserId = userId;
        Type = type.Trim();
        Provider = provider.Trim();
        Last4 = last4.Trim();
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        HolderName = string.IsNullOrWhiteSpace(holderName) ? null : holderName.Trim();
        IsDefault = isDefault;
        CreatedAtUtc = EnsureUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string Last4 { get; private set; } = string.Empty;
    public int ExpiryMonth { get; private set; }
    public int ExpiryYear { get; private set; }
    public string? HolderName { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void SetDefault(bool isDefault, DateTime updatedAtUtc)
    {
        IsDefault = isDefault;
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
namespace AuctionSystem.Infrastructure.Persistence;

public sealed class AdminSystemSetting
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid UpdatedByUserId { get; private set; }

    private AdminSystemSetting()
    {
    }

    public static AdminSystemSetting Create(string key, string value, DateTime updatedAtUtc, Guid updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        if (value is null)
        {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        var normalizedUpdatedAt = updatedAtUtc.Kind == DateTimeKind.Utc
            ? updatedAtUtc
            : updatedAtUtc.ToUniversalTime();

        return new AdminSystemSetting
        {
            Key = key.Trim(),
            Value = value.Trim(),
            UpdatedAtUtc = normalizedUpdatedAt,
            UpdatedByUserId = updatedByUserId
        };
    }

    public void Update(string value, DateTime updatedAtUtc, Guid updatedByUserId)
    {
        if (value is null)
        {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        Value = value.Trim();
        UpdatedAtUtc = updatedAtUtc.Kind == DateTimeKind.Utc
            ? updatedAtUtc
            : updatedAtUtc.ToUniversalTime();
        UpdatedByUserId = updatedByUserId;
    }
}

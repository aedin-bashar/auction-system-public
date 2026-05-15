namespace AuctionSystem.Application.Admin.SystemSettings;

public sealed record AdminSystemSettingDto(
    string Key,
    string Value,
    DateTime UpdatedAtUtc,
    Guid UpdatedByUserId);

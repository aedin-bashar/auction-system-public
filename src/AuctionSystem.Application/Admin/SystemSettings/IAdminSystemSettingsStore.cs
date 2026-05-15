namespace AuctionSystem.Application.Admin.SystemSettings;

public interface IAdminSystemSettingsStore
{
    Task<AdminSystemSettingDto> UpsertAsync(UpsertAdminSystemSettingRequest request, CancellationToken cancellationToken = default);
}

public sealed record UpsertAdminSystemSettingRequest(
    string Key,
    string Value,
    Guid UpdatedByUserId,
    DateTime UpdatedAtUtc);

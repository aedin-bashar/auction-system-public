using AuctionSystem.Application.Admin.SystemSettings;
using AuctionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.Infrastructure.Repositories;

public sealed class AdminSystemSettingsStore : IAdminSystemSettingsStore
{
    private readonly ApplicationDbContext _db;

    public AdminSystemSettingsStore(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<AdminSystemSettingDto> UpsertAsync(UpsertAdminSystemSettingRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Key)) throw new ArgumentException("Key is required.", nameof(request));
        if (request.Value is null) throw new ArgumentException("Value is required.", nameof(request));

        var normalizedKey = request.Key.Trim();
        var normalizedValue = request.Value.Trim();
        var updatedAt = request.UpdatedAtUtc.Kind == DateTimeKind.Utc
            ? request.UpdatedAtUtc
            : request.UpdatedAtUtc.ToUniversalTime();

        var existing = await _db.AdminSystemSettings
            .SingleOrDefaultAsync(x => x.Key == normalizedKey, cancellationToken);

        if (existing is null)
        {
            var created = AdminSystemSetting.Create(normalizedKey, normalizedValue, updatedAt, request.UpdatedByUserId);
            _db.AdminSystemSettings.Add(created);
            await _db.SaveChangesAsync(cancellationToken);

            return new AdminSystemSettingDto(
                created.Key,
                created.Value,
                created.UpdatedAtUtc,
                created.UpdatedByUserId);
        }

        existing.Update(normalizedValue, updatedAt, request.UpdatedByUserId);
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminSystemSettingDto(
            existing.Key,
            existing.Value,
            existing.UpdatedAtUtc,
            existing.UpdatedByUserId);
    }
}

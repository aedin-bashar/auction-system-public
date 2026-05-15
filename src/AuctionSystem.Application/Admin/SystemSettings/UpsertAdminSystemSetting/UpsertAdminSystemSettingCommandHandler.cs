using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Admin.SystemSettings.UpsertAdminSystemSetting;

public sealed class UpsertAdminSystemSettingCommandHandler : IRequestHandler<UpsertAdminSystemSettingCommand, AdminSystemSettingDto>
{
    private readonly IUserRepository _users;
    private readonly IAdminSystemSettingsStore _settings;

    public UpsertAdminSystemSettingCommandHandler(IUserRepository users, IAdminSystemSettingsStore settings)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<AdminSystemSettingDto> Handle(UpsertAdminSystemSettingCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requester = await _users.GetByIdAsync(request.RequesterUserId, cancellationToken);
        if (requester is null || !requester.IsActive || requester.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only active administrators can update system settings.");
        }

        return await _settings.UpsertAsync(
            new UpsertAdminSystemSettingRequest(
                request.Key.Trim(),
                request.Value.Trim(),
                request.RequesterUserId,
                DateTime.UtcNow),
            cancellationToken);
    }
}

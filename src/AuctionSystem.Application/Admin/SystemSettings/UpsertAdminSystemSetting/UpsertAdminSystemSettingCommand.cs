using MediatR;

namespace AuctionSystem.Application.Admin.SystemSettings.UpsertAdminSystemSetting;

public sealed record UpsertAdminSystemSettingCommand(
    Guid RequesterUserId,
    string Key,
    string Value) : IRequest<AdminSystemSettingDto>;

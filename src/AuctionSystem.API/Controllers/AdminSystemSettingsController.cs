using AuctionSystem.API.Extensions;
using AuctionSystem.Application.Admin.SystemSettings;
using AuctionSystem.Application.Admin.SystemSettings.UpsertAdminSystemSetting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminSystemSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminSystemSettingsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPut("{key}")]
    [ProducesResponseType(typeof(AdminSystemSettingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminSystemSettingDto>> Upsert(
        string key,
        [FromBody] UpsertAdminSystemSettingRequestBody request,
        CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var setting = await _mediator.Send(
            new UpsertAdminSystemSettingCommand(requesterUserId, key, request.Value),
            cancellationToken);

        return Ok(setting);
    }

    public sealed record UpsertAdminSystemSettingRequestBody(string Value);
}

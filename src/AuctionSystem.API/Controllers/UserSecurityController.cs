using AuctionSystem.API.Extensions;
using AuctionSystem.Application.Users.Security.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/users/security")]
[Authorize]
public sealed class UserSecurityController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserSecurityController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        await _mediator.Send(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword),
            cancellationToken);

        return NoContent();
    }

    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}

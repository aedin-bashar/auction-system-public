using AuctionSystem.API.Extensions;
using AuctionSystem.Application.Users.Profile;
using AuctionSystem.Application.Users.Profile.GetUserProfile;
using AuctionSystem.Application.Users.Profile.UpdateUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/users/profile")]
[Authorize]
public sealed class UserProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserProfileController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var profile = await _mediator.Send(new GetUserProfileQuery(userId), cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var profile = await _mediator.Send(
            new UpdateUserProfileCommand(
                userId,
                request.Email,
                request.FullName,
                request.PhoneNumber),
            cancellationToken);

        return Ok(profile);
    }

    public sealed record UpdateProfileRequest(string Email, string FullName, string? PhoneNumber);
}

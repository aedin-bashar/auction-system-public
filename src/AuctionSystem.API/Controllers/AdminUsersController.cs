using AuctionSystem.API.Extensions;
using AuctionSystem.Application.Users.Admin.UserManagement;
using AuctionSystem.Application.Users.Admin.UserManagement.DeleteUserByAdmin;
using AuctionSystem.Application.Users.Admin.UserManagement.GetAdminUsers;
using AuctionSystem.Application.Users.Admin.UserManagement.UpdateAdminUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var users = await _mediator.Send(new GetAdminUsersQuery(requesterUserId), cancellationToken);
        return Ok(users);
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(
        Guid userId,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();

        var result = await _mediator.Send(
            new UpdateAdminUserCommand(
                requesterUserId,
                userId,
                request.Email,
                request.FullName,
                request.PhoneNumber,
                request.Role,
                request.IsActive),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();

        await _mediator.Send(new DeleteUserByAdminCommand(requesterUserId, userId), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateAdminUserRequest(
        string Email,
        string FullName,
        string? PhoneNumber,
        string Role,
        bool IsActive);
}

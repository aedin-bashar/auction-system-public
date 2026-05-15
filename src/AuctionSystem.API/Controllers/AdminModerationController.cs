using AuctionSystem.API.Extensions;
using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Application.Admin.Moderation.GetFlaggedCases;
using AuctionSystem.Application.Admin.Moderation.ResolveFlaggedCase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/admin/moderation/cases")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminModerationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminModerationController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminFlaggedCaseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminFlaggedCaseDto>>> GetCases(
        [FromQuery] bool includeResolved = false,
        CancellationToken cancellationToken = default)
    {
        var requesterUserId = User.GetRequiredUserId();
        var cases = await _mediator.Send(new GetFlaggedCasesQuery(requesterUserId, includeResolved), cancellationToken);
        return Ok(cases);
    }

    [HttpPost("{caseId:guid}/resolve")]
    [ProducesResponseType(typeof(AdminFlaggedCaseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminFlaggedCaseDto>> ResolveCase(
        Guid caseId,
        [FromBody] ResolveFlaggedCaseRequestBody request,
        CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var result = await _mediator.Send(
            new ResolveFlaggedCaseCommand(requesterUserId, caseId, request.ResolutionNote),
            cancellationToken);

        return Ok(result);
    }

    public sealed record ResolveFlaggedCaseRequestBody(string? ResolutionNote);
}
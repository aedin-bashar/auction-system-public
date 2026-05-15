using AuctionSystem.API.Extensions;
using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Application.Admin.Reports.GetAdminDashboard;
using AuctionSystem.Application.Admin.Reports.GenerateAdminReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminReportsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var dashboard = await _mediator.Send(new GetAdminDashboardQuery(requesterUserId), cancellationToken);
        return Ok(dashboard);
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(AdminReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminReportDto>> Generate(
        [FromBody] GenerateAdminReportRequestBody request,
        CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var report = await _mediator.Send(
            new GenerateAdminReportCommand(
                requesterUserId,
                request.ReportType,
                request.RangeStartUtc,
                request.RangeEndUtc),
            cancellationToken);

        return Ok(report);
    }

    public sealed record GenerateAdminReportRequestBody(
        string ReportType,
        DateTime RangeStartUtc,
        DateTime RangeEndUtc);
}

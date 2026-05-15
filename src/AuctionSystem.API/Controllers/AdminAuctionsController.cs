using AuctionSystem.API.Extensions;
using AuctionSystem.Application.Auctions.Admin.AuctionManagement;
using AuctionSystem.Application.Auctions.Admin.AuctionManagement.DeleteAuctionByAdmin;
using AuctionSystem.Application.Auctions.Admin.AuctionManagement.EndAuctionByAdmin;
using AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctionDetail;
using AuctionSystem.Application.Auctions.Admin.AuctionManagement.GetAdminAuctions;
using AuctionSystem.Application.Auctions.Admin.AuctionManagement.StartAuctionByAdmin;
using AuctionSystem.Application.Auctions.Admin.AuctionManagement.UpdateAuctionByAdmin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/admin/auctions")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminAuctionsController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly IMediator _mediator;

    public AdminAuctionsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminAuctionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminAuctionListItemDto>>> GetAuctions(CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var auctions = await _mediator.Send(new GetAdminAuctionsQuery(requesterUserId), cancellationToken);
        return Ok(auctions);
    }

    [HttpGet("{auctionId:guid}")]
    [ProducesResponseType(typeof(AdminAuctionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminAuctionDetailDto>> GetAuction(Guid auctionId, CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var auction = await _mediator.Send(new GetAdminAuctionDetailQuery(requesterUserId, auctionId), cancellationToken);
        return Ok(auction);
    }

    [HttpPost("{auctionId:guid}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EndAuction(Guid auctionId, CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        await _mediator.Send(new EndAuctionByAdminCommand(requesterUserId, auctionId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{auctionId:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartAuction(Guid auctionId, CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        await _mediator.Send(new StartAuctionByAdminCommand(requesterUserId, auctionId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{auctionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAuction(Guid auctionId, CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        await _mediator.Send(new DeleteAuctionByAdminCommand(requesterUserId, auctionId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{auctionId:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAuction(
        Guid auctionId,
        [FromForm] UpdateAuctionFormRequest request,
        CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var images = await ReadAuctionImagesAsync(request.Images, cancellationToken);

        await _mediator.Send(
            new UpdateAuctionByAdminCommand(
                requesterUserId,
                auctionId,
                request.Title,
                request.Category,
                request.Description,
                request.StartingPriceAmount,
                request.Currency,
                request.EndTimeUtc,
                request.ReplaceImages,
                images),
            cancellationToken);

        return NoContent();
    }

    public sealed record UpdateAuctionFormRequest(
        string Title,
        string Category,
        string? Description,
        decimal StartingPriceAmount,
        string Currency,
        DateTime EndTimeUtc,
        bool ReplaceImages,
        List<IFormFile>? Images);

    private async Task<IReadOnlyList<UpdateAuctionImageInput>> ReadAuctionImagesAsync(List<IFormFile>? images, CancellationToken cancellationToken)
    {
        if (images is null || images.Count == 0)
        {
            return Array.Empty<UpdateAuctionImageInput>();
        }

        var result = new List<UpdateAuctionImageInput>(images.Count);

        for (var index = 0; index < images.Count; index++)
        {
            var image = images[index];
            if (image is null || image.Length == 0)
            {
                continue;
            }

            var extension = Path.GetExtension(image.FileName);
            if (!AllowedImageExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Unsupported image format. Allowed: .jpg, .jpeg, .png, .webp, .gif");
            }

            await using var ms = new MemoryStream();
            await image.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();
            if (bytes.Length == 0)
            {
                continue;
            }

            result.Add(new UpdateAuctionImageInput(
                image.FileName,
                image.ContentType,
                bytes,
                index));
        }

        return result;
    }
}

using AuctionSystem.Application.Auctions.GetActiveAuctions;
using AuctionSystem.Application.Auctions.MyBids;
using AuctionSystem.Application.Auctions.PlaceBid;
using AuctionSystem.Application.Auctions.ReportAuction;
using AuctionSystem.Application.Auctions.CreateAuction;
using AuctionSystem.API.Extensions;
using AuctionSystem.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/auctions")]
public sealed class AuctionsController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly IMediator _mediator;
    private readonly IAuctionRepository _auctions;

    public AuctionsController(IMediator mediator, IAuctionRepository auctions)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _auctions = auctions ?? throw new ArgumentNullException(nameof(auctions));
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<GetActiveAuctionsItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetActiveAuctionsItemDto>>> GetActiveAuctions(
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var auctions = await _mediator.Send(
            new GetActiveAuctionsQuery(category, minPrice, maxPrice, pageNumber, pageSize),
            cancellationToken);

        return Ok(auctions);
    }

    [HttpGet("{auctionId:guid}/images/{imageId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAuctionImage(Guid auctionId, Guid imageId, CancellationToken cancellationToken)
    {
        var auction = await _auctions.GetWithImagesByIdAsync(auctionId, cancellationToken);
        if (auction is null)
        {
            return NotFound();
        }

        var image = auction.Images.FirstOrDefault(x => x.Id == imageId);
        if (image is null)
        {
            return NotFound();
        }

        return File(image.Content, image.ContentType, enableRangeProcessing: true);
    }

    [HttpPost("{auctionId:guid}/bids")]
    [Authorize]
    [ProducesResponseType(typeof(PlaceBidResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlaceBidResultDto>> PlaceBid(
        Guid auctionId,
        [FromBody] PlaceBidRequest request,
        CancellationToken cancellationToken)
    {
        var bidderUserId = User.GetRequiredUserId();

        var result = await _mediator.Send(
            new PlaceBidCommand(
                auctionId,
                bidderUserId,
                request.Amount,
                request.Currency),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{auctionId:guid}/reports")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> ReportAuction(
        Guid auctionId,
        [FromBody] ReportAuctionRequest request,
        CancellationToken cancellationToken)
    {
        var reporterUserId = User.GetRequiredUserId();
        var caseId = await _mediator.Send(
            new ReportAuctionCommand(
                auctionId,
                reporterUserId,
                request.Reason,
                request.Details),
            cancellationToken);

        return Ok(caseId);
    }

    [HttpGet("my-bids")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<MyBidItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyBidItemDto>>> GetMyBids(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var bids = await _mediator.Send(new GetMyBidsQuery(userId), cancellationToken);
        return Ok(bids);
    }

    public sealed record PlaceBidRequest(
        decimal Amount,
        string Currency);

    public sealed record ReportAuctionRequest(
        string Reason,
        string? Details);

    [HttpPost]
    [Authorize]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> CreateAuction(
        [FromBody] CreateAuctionRequest request,
        CancellationToken cancellationToken)
    {
        var sellerId = User.GetRequiredUserId();

        var auctionId = await _mediator.Send(
            new CreateAuctionCommand(
                sellerId,
                request.Title,
                request.Category,
                request.Description,
                request.StartingPriceAmount,
                request.Currency,
                request.EndTimeUtc),
            cancellationToken);

        return Ok(auctionId);
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> CreateAuctionWithImage(
        [FromForm] CreateAuctionFormRequest request,
        CancellationToken cancellationToken)
    {
        var sellerId = User.GetRequiredUserId();
        var images = await ReadAuctionImagesAsync(request.Images, cancellationToken);

        var auctionId = await _mediator.Send(
            new CreateAuctionCommand(
                sellerId,
                request.Title,
                request.Category,
                request.Description,
                request.StartingPriceAmount,
                request.Currency,
                request.EndTimeUtc,
                images),
            cancellationToken);

        return Ok(auctionId);
    }

    public sealed record CreateAuctionRequest(
        string Title,
        string Category,
        string? Description,
        decimal StartingPriceAmount,
        string Currency,
        DateTime EndTimeUtc);

    public sealed record CreateAuctionFormRequest(
        string Title,
        string Category,
        string? Description,
        decimal StartingPriceAmount,
        string Currency,
        DateTime EndTimeUtc,
        List<IFormFile>? Images);

    private async Task<IReadOnlyList<CreateAuctionImageInput>> ReadAuctionImagesAsync(List<IFormFile>? images, CancellationToken cancellationToken)
    {
        if (images is null || images.Count == 0)
        {
            return Array.Empty<CreateAuctionImageInput>();
        }

        var result = new List<CreateAuctionImageInput>(images.Count);

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

            result.Add(new CreateAuctionImageInput(
                image.FileName,
                image.ContentType,
                bytes,
                index));
        }

        return result;
    }
}

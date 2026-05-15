using System.IdentityModel.Tokens.Jwt;
using AuctionSystem.API.DTOs;
using AuctionSystem.API.Hubs;
using AuctionSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/auctions/{auctionId}/bids")]
public class BidsController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly IHubContext<AuctionHub> _hubContext;

    public BidsController(IAuctionService auctionService, IHubContext<AuctionHub> hubContext)
    {
        _auctionService = auctionService;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetBids(int auctionId) =>
        Ok(await _auctionService.GetBidsForAuction(auctionId));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceBid(int auctionId, [FromBody] PlaceBidDto dto)
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var bid = await _auctionService.PlaceBid(auctionId, userId, dto.Amount);
            var username = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "Unknown";
            await _hubContext.Clients.Group($"auction-{auctionId}")
                .SendAsync("BidUpdate", auctionId, bid.Amount, username);
            return Ok(bid);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

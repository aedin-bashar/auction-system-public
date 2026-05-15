using AuctionSystem.API.DTOs;
using AuctionSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;

    public AuctionsController(IAuctionService auctionService)
    {
        _auctionService = auctionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _auctionService.GetAllAuctions());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var auction = await _auctionService.GetAuctionById(id);
        return auction == null ? NotFound() : Ok(auction);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAuctionDto dto)
    {
        var auction = await _auctionService.CreateAuction(dto);
        return CreatedAtAction(nameof(GetById), new { id = auction.Id }, auction);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAuctionDto dto)
    {
        var updated = await _auctionService.UpdateAuction(id, dto);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _auctionService.DeleteAuction(id);
        return deleted ? NoContent() : NotFound();
    }
}

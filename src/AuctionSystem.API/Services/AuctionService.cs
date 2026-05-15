using AuctionSystem.API.Data;
using AuctionSystem.API.DTOs;
using AuctionSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.API.Services;

public class AuctionService : IAuctionService
{
    private readonly AuctionDbContext _context;
    public AuctionService(AuctionDbContext context) { _context = context; }

    public async Task<List<Auction>> GetAllAuctions() => await _context.Auctions.Include(a => a.Bids).ToListAsync();
    public async Task<Auction?> GetAuctionById(int id) => await _context.Auctions.Include(a => a.Bids).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Auction> CreateAuction(CreateAuctionDto dto, int createdByUserId)
    {
        var auction = new Auction
        {
            Title = dto.Title,
            Description = dto.Description,
            StartingPrice = dto.StartingPrice,
            CurrentPrice = dto.StartingPrice,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            CreatedByUserId = createdByUserId
        };
        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync();
        return auction;
    }

    public async Task<bool> UpdateAuction(int id, UpdateAuctionDto dto)
    {
        var auction = await _context.Auctions.FindAsync(id);
        if (auction == null) return false;
        auction.Title = dto.Title;
        auction.Description = dto.Description;
        auction.StartTime = dto.StartTime;
        auction.EndTime = dto.EndTime;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAuction(int id)
    {
        var auction = await _context.Auctions.FindAsync(id);
        if (auction == null) return false;
        _context.Auctions.Remove(auction);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Bid> PlaceBid(int auctionId, int userId, decimal amount)
    {
        var auction = await _context.Auctions.FindAsync(auctionId) ?? throw new Exception("Auction not found");
        if (auction.EndTime <= DateTime.UtcNow) throw new Exception("Auction has ended");
        if (amount <= auction.CurrentPrice) throw new Exception("Bid must be higher than current price");
        var bid = new Bid { AuctionId = auctionId, UserId = userId, Amount = amount, PlacedAt = DateTime.UtcNow };
        auction.CurrentPrice = amount;
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        return bid;
    }

    public async Task<List<Bid>> GetBidsForAuction(int auctionId) =>
        await _context.Bids.Where(b => b.AuctionId == auctionId).Include(b => b.User).ToListAsync();
}

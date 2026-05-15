using AuctionSystem.API.DTOs;
using AuctionSystem.API.Models;

namespace AuctionSystem.API.Services;

public interface IAuctionService
{
    Task<List<Auction>> GetAllAuctions();
    Task<Auction?> GetAuctionById(int id);
    Task<Auction> CreateAuction(CreateAuctionDto dto);
    Task<bool> UpdateAuction(int id, UpdateAuctionDto dto);
    Task<bool> DeleteAuction(int id);
    Task<Bid> PlaceBid(int auctionId, int userId, decimal amount);
    Task<List<Bid>> GetBidsForAuction(int auctionId);
}

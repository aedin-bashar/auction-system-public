namespace AuctionSystem.API.Models;

public class Auction
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int CreatedByUserId { get; set; }
    public User? User { get; set; }
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public bool IsActive => EndTime > DateTime.UtcNow;
}

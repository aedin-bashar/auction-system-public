using AuctionSystem.API.Models;

namespace AuctionSystem.API.Services;

public interface IAuthService
{
    Task<User?> Register(string username, string password, string role);
    Task<string?> Login(string username, string password);
    string GenerateJwtToken(User user);
}

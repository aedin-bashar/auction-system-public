namespace AuctionSystem.Application.Authentication.Models;

public sealed record TokenRequest(
    Guid UserId,
    string Email,
    string Role);

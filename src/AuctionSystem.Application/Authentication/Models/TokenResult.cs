namespace AuctionSystem.Application.Authentication.Models;

public sealed record TokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);

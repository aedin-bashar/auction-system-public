namespace AuctionSystem.Application.Authentication.Models;

public sealed record LoginResultDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string AccessToken,
    DateTime ExpiresAtUtc);

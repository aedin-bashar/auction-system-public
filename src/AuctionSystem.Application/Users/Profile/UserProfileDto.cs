namespace AuctionSystem.Application.Users.Profile;

public sealed record UserProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

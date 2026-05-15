namespace AuctionSystem.Application.Users.Admin.UserManagement;

public sealed record AdminUserDto(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

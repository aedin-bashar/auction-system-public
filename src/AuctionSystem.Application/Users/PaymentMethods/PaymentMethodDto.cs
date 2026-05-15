namespace AuctionSystem.Application.Users.PaymentMethods;

public sealed record PaymentMethodDto(
    Guid Id,
    Guid UserId,
    string Type,
    string Provider,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string? HolderName,
    bool IsDefault,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

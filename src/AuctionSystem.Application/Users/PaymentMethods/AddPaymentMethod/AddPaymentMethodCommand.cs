using MediatR;

namespace AuctionSystem.Application.Users.PaymentMethods.AddPaymentMethod;

public sealed record AddPaymentMethodCommand(
    Guid UserId,
    string Type,
    string Provider,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string? HolderName,
    bool IsDefault) : IRequest<PaymentMethodDto>;

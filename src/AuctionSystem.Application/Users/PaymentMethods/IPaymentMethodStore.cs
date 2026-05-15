namespace AuctionSystem.Application.Users.PaymentMethods;

public interface IPaymentMethodStore
{
    Task<PaymentMethodDto> AddAsync(AddPaymentMethodRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentMethodDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken = default);
}

public sealed record AddPaymentMethodRequest(
    Guid UserId,
    string Type,
    string Provider,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string? HolderName,
    bool IsDefault);

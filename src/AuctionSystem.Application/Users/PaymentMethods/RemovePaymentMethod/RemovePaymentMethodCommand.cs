using MediatR;

namespace AuctionSystem.Application.Users.PaymentMethods.RemovePaymentMethod;

public sealed record RemovePaymentMethodCommand(Guid UserId, Guid PaymentMethodId) : IRequest<Unit>;

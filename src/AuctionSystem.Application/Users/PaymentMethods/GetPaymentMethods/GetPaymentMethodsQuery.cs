using MediatR;

namespace AuctionSystem.Application.Users.PaymentMethods.GetPaymentMethods;

public sealed record GetPaymentMethodsQuery(Guid UserId) : IRequest<IReadOnlyList<PaymentMethodDto>>;

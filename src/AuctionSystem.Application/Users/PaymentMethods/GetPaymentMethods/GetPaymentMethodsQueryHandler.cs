using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Users.PaymentMethods.GetPaymentMethods;

public sealed class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>
{
    private readonly IUserRepository _users;
    private readonly IPaymentMethodStore _paymentMethods;

    public GetPaymentMethodsQueryHandler(IUserRepository users, IPaymentMethodStore paymentMethods)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _paymentMethods = paymentMethods ?? throw new ArgumentNullException(nameof(paymentMethods));
    }

    public async Task<IReadOnlyList<PaymentMethodDto>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User was not found.");
        }

        return await _paymentMethods.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}

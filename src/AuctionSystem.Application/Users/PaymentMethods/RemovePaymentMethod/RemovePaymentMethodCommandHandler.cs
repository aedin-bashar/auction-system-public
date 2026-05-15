using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Users.PaymentMethods.RemovePaymentMethod;

public sealed class RemovePaymentMethodCommandHandler : IRequestHandler<RemovePaymentMethodCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IPaymentMethodStore _paymentMethods;
    private readonly IUnitOfWork _unitOfWork;

    public RemovePaymentMethodCommandHandler(
        IUserRepository users,
        IPaymentMethodStore paymentMethods,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _paymentMethods = paymentMethods ?? throw new ArgumentNullException(nameof(paymentMethods));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Unit> Handle(RemovePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User was not found.");
        }

        var removed = await _paymentMethods.RemoveAsync(request.UserId, request.PaymentMethodId, cancellationToken);
        if (!removed)
        {
            throw new KeyNotFoundException("Payment method was not found.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

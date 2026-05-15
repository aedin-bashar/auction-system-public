using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Users.PaymentMethods.AddPaymentMethod;

public sealed class AddPaymentMethodCommandHandler : IRequestHandler<AddPaymentMethodCommand, PaymentMethodDto>
{
    private readonly IUserRepository _users;
    private readonly IPaymentMethodStore _paymentMethods;
    private readonly IUnitOfWork _unitOfWork;

    public AddPaymentMethodCommandHandler(
        IUserRepository users,
        IPaymentMethodStore paymentMethods,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _paymentMethods = paymentMethods ?? throw new ArgumentNullException(nameof(paymentMethods));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PaymentMethodDto> Handle(AddPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User was not found.");
        }

        var created = await _paymentMethods.AddAsync(
            new AddPaymentMethodRequest(
                request.UserId,
                request.Type,
                request.Provider,
                request.Last4,
                request.ExpiryMonth,
                request.ExpiryYear,
                request.HolderName,
                request.IsDefault),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return created;
    }
}

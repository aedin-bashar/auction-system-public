using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Application.Users.PaymentMethods.GetPaymentMethods;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.PaymentMethods;

public class GetPaymentMethodsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingUser_ReturnsPaymentMethods()
    {
        var user = User.Register("user@example.com", "Payment User", UserRole.Bidder);
        var query = new GetPaymentMethodsQuery(user.Id);
        var now = DateTime.UtcNow;
        IReadOnlyList<PaymentMethodDto> methods =
        [
            new PaymentMethodDto(Guid.NewGuid(), user.Id, "Card", "Visa", "4242", 12, 2030, "Payment User", true, now, now)
        ];

        var users = new Mock<IUserRepository>();
        var paymentMethods = new Mock<IPaymentMethodStore>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        paymentMethods.Setup(x => x.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(methods);

        var handler = new GetPaymentMethodsQueryHandler(users.Object, paymentMethods.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(methods, result);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var query = new GetPaymentMethodsQuery(Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var paymentMethods = new Mock<IPaymentMethodStore>();

        users.Setup(x => x.GetByIdAsync(query.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var handler = new GetPaymentMethodsQueryHandler(users.Object, paymentMethods.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));

        paymentMethods.VerifyNoOtherCalls();
    }
}
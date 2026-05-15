using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Application.Users.PaymentMethods.AddPaymentMethod;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.PaymentMethods;

public class AddPaymentMethodCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidUser_AddsPaymentMethodAndPersists()
    {
        var user = User.Register("user@example.com", "Payment User", UserRole.Bidder);
        var now = DateTime.UtcNow;
        var command = new AddPaymentMethodCommand(user.Id, "Card", "Visa", "4242", 12, 2030, "Payment User", true);
        var created = new PaymentMethodDto(
            Guid.NewGuid(),
            user.Id,
            "Card",
            "Visa",
            "4242",
            12,
            2030,
            "Payment User",
            true,
            now,
            now);

        var users = new Mock<IUserRepository>();
        var paymentMethods = new Mock<IPaymentMethodStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        paymentMethods.Setup(x => x.AddAsync(
                It.Is<AddPaymentMethodRequest>(r =>
                    r.UserId == user.Id &&
                    r.Type == command.Type &&
                    r.Provider == command.Provider &&
                    r.Last4 == command.Last4 &&
                    r.ExpiryMonth == command.ExpiryMonth &&
                    r.ExpiryYear == command.ExpiryYear &&
                    r.HolderName == command.HolderName &&
                    r.IsDefault == command.IsDefault),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AddPaymentMethodCommandHandler(users.Object, paymentMethods.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(created, result);
        paymentMethods.Verify(x => x.AddAsync(It.IsAny<AddPaymentMethodRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var command = new AddPaymentMethodCommand(Guid.NewGuid(), "Card", "Visa", "4242", 12, 2030, "Payment User", true);

        var users = new Mock<IUserRepository>();
        var paymentMethods = new Mock<IPaymentMethodStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new AddPaymentMethodCommandHandler(users.Object, paymentMethods.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        paymentMethods.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
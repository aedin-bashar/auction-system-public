using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Application.Users.PaymentMethods.RemovePaymentMethod;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;
using Moq;

namespace AuctionSystem.UnitTests.Application.Users.PaymentMethods;

public class RemovePaymentMethodCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingPaymentMethod_RemovesAndPersists()
    {
        var user = User.Register("user@example.com", "Payment User", UserRole.Bidder);
        var paymentMethodId = Guid.NewGuid();
        var command = new RemovePaymentMethodCommand(user.Id, paymentMethodId);

        var users = new Mock<IUserRepository>();
        var paymentMethods = new Mock<IPaymentMethodStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        paymentMethods.Setup(x => x.RemoveAsync(user.Id, paymentMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RemovePaymentMethodCommandHandler(users.Object, paymentMethods.Object, unitOfWork.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        paymentMethods.Verify(x => x.RemoveAsync(user.Id, paymentMethodId, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var command = new RemovePaymentMethodCommand(Guid.NewGuid(), Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var paymentMethods = new Mock<IPaymentMethodStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new RemovePaymentMethodCommandHandler(users.Object, paymentMethods.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        paymentMethods.VerifyNoOtherCalls();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodNotFound_ThrowsKeyNotFoundException()
    {
        var user = User.Register("user@example.com", "Payment User", UserRole.Bidder);
        var command = new RemovePaymentMethodCommand(user.Id, Guid.NewGuid());

        var users = new Mock<IUserRepository>();
        var paymentMethods = new Mock<IPaymentMethodStore>();
        var unitOfWork = new Mock<IUnitOfWork>();

        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        paymentMethods.Setup(x => x.RemoveAsync(user.Id, command.PaymentMethodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new RemovePaymentMethodCommandHandler(users.Object, paymentMethods.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));

        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
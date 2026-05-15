namespace AuctionSystem.UnitTests.Application.Payments.Admin.TransactionManagement;

public class ProcessRefundCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var validator = new AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund.ProcessRefundCommandValidator();
        var command = new AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund.ProcessRefundCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Duplicate charge");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_Fails()
    {
        var validator = new AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund.ProcessRefundCommandValidator();
        var command = new AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund.ProcessRefundCommand(
            Guid.Empty,
            Guid.Empty,
            null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenReasonIsTooLong_Fails()
    {
        var validator = new AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund.ProcessRefundCommandValidator();
        var command = new AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund.ProcessRefundCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('x', 501));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
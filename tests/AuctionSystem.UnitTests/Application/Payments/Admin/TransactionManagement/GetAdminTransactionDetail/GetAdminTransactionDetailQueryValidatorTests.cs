using AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactionDetail;

namespace AuctionSystem.UnitTests.Application.Payments.Admin.TransactionManagement;

public class GetAdminTransactionDetailQueryValidatorTests
{
    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var validator = new GetAdminTransactionDetailQueryValidator();
        var query = new GetAdminTransactionDetailQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_Fails()
    {
        var validator = new GetAdminTransactionDetailQueryValidator();
        var query = new GetAdminTransactionDetailQuery(Guid.Empty, Guid.Empty);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
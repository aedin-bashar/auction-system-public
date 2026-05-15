using AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactions;

namespace AuctionSystem.UnitTests.Application.Payments.Admin.TransactionManagement;

public class GetAdminTransactionsQueryValidatorTests
{
    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var validator = new GetAdminTransactionsQueryValidator();
        var query = new GetAdminTransactionsQuery(Guid.NewGuid());

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRequesterIdEmpty_Fails()
    {
        var validator = new GetAdminTransactionsQueryValidator();
        var query = new GetAdminTransactionsQuery(Guid.Empty);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
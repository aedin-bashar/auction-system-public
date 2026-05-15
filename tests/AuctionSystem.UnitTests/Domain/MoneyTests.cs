using AuctionSystem.Domain.ValueObjects;

namespace AuctionSystem.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidValues_NormalizesCurrency()
    {
        var money = Money.Create(10.50m, "usd");

        Assert.Equal(10.50m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(-1m, "USD"));
    }

    [Fact]
    public void Create_WithInvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => Money.Create(1m, "US"));
    }

    [Fact]
    public void Add_WithDifferentCurrency_Throws()
    {
        var usd = Money.Create(1m, "USD");
        var eur = Money.Create(1m, "EUR");

        Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum_AndDoesNotMutateOperands()
    {
        var a = Money.Create(10m, "USD");
        var b = Money.Create(2.50m, "USD");

        var result = a.Add(b);

        Assert.Equal(12.50m, result.Amount);
        Assert.Equal("USD", result.Currency);

        // Value object should be immutable-ish: operands unchanged
        Assert.Equal(10m, a.Amount);
        Assert.Equal("USD", a.Currency);
        Assert.Equal(2.50m, b.Amount);
        Assert.Equal("USD", b.Currency);
    }

    [Fact]
    public void Subtract_WithSameCurrency_ReturnsDifference_AndDoesNotMutateOperands()
    {
        var a = Money.Create(10m, "USD");
        var b = Money.Create(3m, "USD");

        var result = a.Subtract(b);

        Assert.Equal(7m, result.Amount);
        Assert.Equal("USD", result.Currency);

        Assert.Equal(10m, a.Amount);
        Assert.Equal(3m, b.Amount);
    }

    [Fact]
    public void Subtract_ResultingZero_IsAllowed()
    {
        var a = Money.Create(5m, "USD");
        var b = Money.Create(5m, "USD");

        var result = a.Subtract(b);

        Assert.Equal(0m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Subtract_ResultingNegative_Throws()
    {
        var a = Money.Create(5m, "USD");
        var b = Money.Create(6m, "USD");

        Assert.Throws<InvalidOperationException>(() => a.Subtract(b));
    }

    [Fact]
    public void Equals_WithSameAmountAndCurrency_IsTrue()
    {
        var a = Money.Create(5m, "USD");
        var b = Money.Create(5m, "USD");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}
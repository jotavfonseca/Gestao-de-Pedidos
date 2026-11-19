using Orders.Core.Domain.Orders;

namespace Orders.Tests.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.004)]
    public void Rejects_non_positive_values(decimal value)
    {
        var ex = Assert.Throws<DomainValidationException>(() => Money.Of(value));
        Assert.Equal("amount", ex.Field);
    }

    [Theory]
    [InlineData(10.005, 10.01)]
    [InlineData(10.004, 10.00)]
    [InlineData(199.9, 199.90)]
    public void Rounds_to_two_decimals_away_from_zero(decimal input, decimal expected)
    {
        Assert.Equal(expected, Money.Of(input).Value);
    }

    [Fact]
    public void Has_value_equality()
    {
        Assert.Equal(Money.Of(10m), Money.Of(10.00m));
        Assert.NotEqual(Money.Of(10m), Money.Of(10.01m));
    }
}

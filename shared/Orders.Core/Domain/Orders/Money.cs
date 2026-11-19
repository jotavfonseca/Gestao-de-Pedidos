using System.Globalization;

namespace Orders.Core.Domain.Orders;

/// <summary>
/// Monetary amount of an order: always positive, with 2 decimal places.
/// Value equality (record).
/// </summary>
public sealed record Money
{
    public decimal Value { get; }

    private Money(decimal value) => Value = value;

    public static Money Of(decimal value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        if (rounded <= 0)
        {
            throw new DomainValidationException("amount", "Amount must be greater than zero.");
        }

        return new Money(rounded);
    }

    public override string ToString() => Value.ToString("0.00", CultureInfo.InvariantCulture);
}

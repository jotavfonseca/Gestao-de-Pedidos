using System.Globalization;

namespace Orders.Core.Domain.Orders;

/// <summary>
/// Valor monetário do pedido: sempre positivo e com 2 casas decimais.
/// Igualdade por valor (record).
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Money Of(decimal value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        if (rounded <= 0)
        {
            throw new DomainValidationException("valor", "O valor deve ser maior que zero.");
        }

        return new Money(rounded);
    }

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture);
}

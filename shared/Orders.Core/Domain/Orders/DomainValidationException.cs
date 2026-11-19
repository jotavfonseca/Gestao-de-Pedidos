using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Domain.Orders;

public sealed class DomainValidationException(string field, string message) : DomainException(message)
{
    /// <summary>Nome do campo (camelCase, como no contrato da API).</summary>
    public string Field { get; } = field;
}

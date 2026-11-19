using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Domain.Orders;

public sealed class DomainValidationException(string field, string message) : DomainException(message)
{
    /// <summary>Field name (camelCase, as in the API contract).</summary>
    public string Field { get; } = field;
}

using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Domain.Orders;

public sealed class InvalidStatusTransitionException(Guid orderId, OrderStatus from, OrderStatus to)
    : DomainException($"Invalid status transition for order {orderId}: {from} → {to}. Allowed sequence: Pending → Processing → Completed.")
{
    public Guid OrderId { get; } = orderId;
    public OrderStatus From { get; } = from;
    public OrderStatus To { get; } = to;
}

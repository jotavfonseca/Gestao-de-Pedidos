using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Domain.Orders.Events;

public sealed record OrderStatusChanged(
    Guid OrderId,
    OrderStatus De,
    OrderStatus Para,
    DateTimeOffset OccurredAt) : IDomainEvent;

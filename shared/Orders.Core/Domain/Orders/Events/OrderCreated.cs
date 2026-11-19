using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Domain.Orders.Events;

public sealed record OrderCreated(
    Guid OrderId,
    string Customer,
    string Product,
    decimal Amount,
    DateTimeOffset OccurredAt) : IDomainEvent;

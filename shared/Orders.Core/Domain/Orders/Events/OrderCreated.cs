using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Domain.Orders.Events;

public sealed record OrderCreated(
    Guid OrderId,
    string Cliente,
    string Produto,
    decimal Valor,
    DateTimeOffset OccurredAt) : IDomainEvent;

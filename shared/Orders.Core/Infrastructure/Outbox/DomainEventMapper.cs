using Orders.Core.Contracts;
using Orders.Core.Domain.Abstractions;
using Orders.Core.Domain.Orders.Events;

namespace Orders.Core.Infrastructure.Outbox;

/// <summary>Translates (internal) domain events into integration messages (contracts).</summary>
internal static class DomainEventMapper
{
    public static OutboxMessage ToOutboxMessage(IDomainEvent domainEvent, string? traceParent, DateTimeOffset now) =>
        domainEvent switch
        {
            OrderCreated e => OutboxMessage.Create(
                MessagingConstants.EventTypes.OrderCreated,
                MessagingConstants.Queues.OrderCreated,
                e.OrderId.ToString(),
                MessageSerializer.Serialize(new OrderCreatedMessage(e.OrderId, e.Customer, e.Product, e.Amount, e.OccurredAt)),
                traceParent,
                now),

            OrderStatusChanged e => OutboxMessage.Create(
                MessagingConstants.EventTypes.OrderStatusChanged,
                MessagingConstants.Queues.OrderStatusChanged,
                e.OrderId.ToString(),
                MessageSerializer.Serialize(new OrderStatusChangedMessage(e.OrderId, e.From.ToString(), e.To.ToString(), e.OccurredAt)),
                traceParent,
                now),

            _ => throw new InvalidOperationException(
                $"No message mapping for domain event {domainEvent.GetType().Name}.")
        };
}

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Orders.Core.Application.Orders;
using Orders.Core.Contracts;
using Orders.Core.Infrastructure.Messaging;

namespace Orders.Worker;

/// <summary>
/// Adapter between the queue and the use case: validates the message, calls
/// <see cref="ProcessOrderHandler"/> (idempotent) and decides how to settle it.
/// </summary>
public sealed class OrderCreatedHandler(ProcessOrderHandler processOrder, ILogger<OrderCreatedHandler> logger)
{
    public async Task<ConsumeResult> HandleAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        message.ApplicationProperties.TryGetValue(MessagingConstants.Properties.EventType, out var eventType);
        if (eventType as string != MessagingConstants.EventTypes.OrderCreated)
        {
            return ConsumeResult.ToDeadLetter("UnexpectedEventType",
                $"EventType '{eventType ?? "(missing)"}' is not handled on queue {MessagingConstants.Queues.OrderCreated}.");
        }

        OrderCreatedMessage? payload;
        try
        {
            payload = MessageSerializer.Deserialize<OrderCreatedMessage>(message.Body);
        }
        catch (JsonException ex)
        {
            return ConsumeResult.ToDeadLetter("InvalidBody", $"Invalid JSON: {ex.Message}");
        }

        if (payload is null || payload.OrderId == Guid.Empty)
        {
            return ConsumeResult.ToDeadLetter("InvalidBody", "Message has no OrderId.");
        }

        if (message.CorrelationId != payload.OrderId.ToString())
        {
            logger.LogWarning("CorrelationId {CorrelationId} differs from the message OrderId {OrderId}.",
                message.CorrelationId, payload.OrderId);
        }

        var outcome = await processOrder.HandleAsync(payload.OrderId, cancellationToken);

        return outcome == ProcessOrderOutcome.OrderNotFound
            ? ConsumeResult.ToDeadLetter("OrderNotFound", $"Order {payload.OrderId} was not found.")
            : ConsumeResult.Complete;
    }
}

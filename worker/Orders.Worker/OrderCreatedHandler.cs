using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Orders.Core.Application.Orders;
using Orders.Core.Contracts;
using Orders.Core.Infrastructure.Messaging;

namespace Orders.Worker;

/// <summary>
/// Adaptador entre a fila e o caso de uso: valida a mensagem, chama
/// <see cref="ProcessOrderHandler"/> (idempotente) e decide como liquidá-la.
/// </summary>
public sealed class OrderCreatedHandler(ProcessOrderHandler processOrder, ILogger<OrderCreatedHandler> logger)
{
    public async Task<ConsumeResult> HandleAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        message.ApplicationProperties.TryGetValue(MessagingConstants.Properties.EventType, out var eventType);
        if (eventType as string != MessagingConstants.EventTypes.OrderCreated)
        {
            return ConsumeResult.ToDeadLetter("UnexpectedEventType",
                $"EventType '{eventType ?? "(ausente)"}' não é tratado na fila {MessagingConstants.Queues.OrderCreated}.");
        }

        OrderCreatedMessage? payload;
        try
        {
            payload = MessageSerializer.Deserialize<OrderCreatedMessage>(message.Body);
        }
        catch (JsonException ex)
        {
            return ConsumeResult.ToDeadLetter("InvalidBody", $"JSON inválido: {ex.Message}");
        }

        if (payload is null || payload.OrderId == Guid.Empty)
        {
            return ConsumeResult.ToDeadLetter("InvalidBody", "Mensagem sem OrderId.");
        }

        if (message.CorrelationId != payload.OrderId.ToString())
        {
            logger.LogWarning("CorrelationId {CorrelationId} difere do OrderId {OrderId} da mensagem.",
                message.CorrelationId, payload.OrderId);
        }

        var outcome = await processOrder.HandleAsync(payload.OrderId, cancellationToken);

        return outcome == ProcessOrderOutcome.OrderNotFound
            ? ConsumeResult.ToDeadLetter("OrderNotFound", $"Pedido {payload.OrderId} não encontrado.")
            : ConsumeResult.Complete;
    }
}

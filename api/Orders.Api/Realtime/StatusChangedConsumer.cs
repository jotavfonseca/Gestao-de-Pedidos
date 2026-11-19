using Azure.Messaging.ServiceBus;
using Orders.Core.Contracts;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Messaging;

namespace Orders.Api.Realtime;

/// <summary>Repassa as mudanças de status publicadas pelo worker aos clientes SignalR.</summary>
internal sealed class StatusChangedConsumer(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    ILogger<StatusChangedConsumer> logger) : QueueConsumer(client, scopeFactory, logger)
{
    protected override string QueueName => MessagingConstants.Queues.OrderStatusChanged;

    protected override async Task<ConsumeResult> HandleAsync(
        ServiceBusReceivedMessage message,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (!message.ApplicationProperties.TryGetValue(MessagingConstants.Properties.EventType, out var eventType)
            || eventType as string != MessagingConstants.EventTypes.OrderStatusChanged)
        {
            return ConsumeResult.ToDeadLetter("UnexpectedEventType", $"EventType '{eventType}' não é tratado nesta fila.");
        }

        var payload = MessageSerializer.Deserialize<OrderStatusChangedMessage>(message.Body);
        if (payload is null || !Enum.TryParse<OrderStatus>(payload.Status, out var status))
        {
            return ConsumeResult.ToDeadLetter("InvalidBody", "Corpo da mensagem inválido.");
        }

        var notifier = services.GetRequiredService<IOrderNotifier>();
        await notifier.OrderStatusChangedAsync(payload.OrderId, status, payload.ChangedAt, cancellationToken);
        return ConsumeResult.Complete;
    }
}

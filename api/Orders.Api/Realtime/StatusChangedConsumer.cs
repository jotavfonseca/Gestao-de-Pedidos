using Azure.Messaging.ServiceBus;
using Orders.Core.Contracts;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Messaging;

namespace Orders.Api.Realtime;

/// <summary>Forwards the status changes published by the worker to SignalR clients.</summary>
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
            return ConsumeResult.ToDeadLetter("UnexpectedEventType", $"EventType '{eventType}' is not handled on this queue.");
        }

        var payload = MessageSerializer.Deserialize<OrderStatusChangedMessage>(message.Body);
        if (payload is null || !Enum.TryParse<OrderStatus>(payload.Status, out var status))
        {
            return ConsumeResult.ToDeadLetter("InvalidBody", "Invalid message body.");
        }

        var notifier = services.GetRequiredService<IOrderNotifier>();
        await notifier.OrderStatusChangedAsync(payload.OrderId, status, payload.ChangedAt, cancellationToken);
        return ConsumeResult.Complete;
    }
}

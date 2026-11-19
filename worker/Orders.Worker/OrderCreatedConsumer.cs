using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Orders.Core.Contracts;
using Orders.Core.Infrastructure.Messaging;

namespace Orders.Worker;

/// <summary>Consumes the order-created queue and delegates each message to <see cref="OrderCreatedHandler"/>.</summary>
internal sealed class OrderCreatedConsumer(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerOptions> options,
    ILogger<OrderCreatedConsumer> logger) : QueueConsumer(client, scopeFactory, logger)
{
    protected override string QueueName => MessagingConstants.Queues.OrderCreated;

    protected override int MaxConcurrentCalls => Math.Max(1, options.Value.MaxConcurrentCalls);

    protected override Task<ConsumeResult> HandleAsync(
        ServiceBusReceivedMessage message,
        IServiceProvider services,
        CancellationToken cancellationToken) =>
        services.GetRequiredService<OrderCreatedHandler>().HandleAsync(message, cancellationToken);
}

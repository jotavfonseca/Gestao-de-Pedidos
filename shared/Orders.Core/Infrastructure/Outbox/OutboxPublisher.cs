using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Core.Contracts;
using Orders.Core.Infrastructure.Messaging;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Core.Infrastructure.Outbox;

/// <summary>
/// Publishes pending outbox messages to Service Bus. Uses
/// FOR UPDATE SKIP LOCKED so several instances never send the same message.
/// At-least-once delivery: duplicates are absorbed by the consumers.
/// </summary>
public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ServiceBusSenders senders,
    TimeProvider clock,
    IOptions<MessagingOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(settings.OutboxIntervalMilliseconds), clock);

        try
        {
            do
            {
                try
                {
                    // Drain the backlog in batches before waiting for the next tick.
                    while (await PublishBatchAsync(settings, stoppingToken) == settings.OutboxBatchSize)
                    {
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Failed to scan the outbox; retrying on the next cycle.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    /// <returns>Number of messages published successfully.</returns>
    internal async Task<int> PublishBatchAsync(MessagingOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var pending = await db.OutboxMessages
            .FromSql($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedAt" IS NULL AND "Attempts" < {settings.OutboxMaxAttempts}
                ORDER BY "CreatedAt"
                LIMIT {settings.OutboxBatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var published = 0;
        foreach (var message in pending)
        {
            try
            {
                await SendAsync(message, cancellationToken);
                message.MarkProcessed(clock.GetUtcNow());
                published++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.MarkFailed(ex.Message);
                logger.LogWarning(ex,
                    "Failed to publish message {MessageId} ({EventType}) for order {OrderId}; attempt {Attempts}.",
                    message.Id, message.Type, message.CorrelationId, message.Attempts);

                if (message.Attempts >= settings.OutboxMaxAttempts)
                {
                    logger.LogError(
                        "Message {MessageId} reached {MaxAttempts} attempts and will stay in the outbox for analysis.",
                        message.Id, settings.OutboxMaxAttempts);
                }

                // Service Bus is probably down: stop this batch and retry on the next cycle.
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (published > 0)
        {
            logger.LogDebug("Published {Count} message(s) from the outbox.", published);
        }

        return published;
    }

    private async Task SendAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var activity = MessagingActivity.Start(
            $"publish {message.Destination}", ActivityKind.Producer, message.TraceParent,
            message.Destination, message.Id.ToString(), message.CorrelationId);

        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(message.Payload))
        {
            MessageId = message.Id.ToString(),
            CorrelationId = message.CorrelationId,
            Subject = message.Type,
            ContentType = "application/json"
        };
        serviceBusMessage.ApplicationProperties[MessagingConstants.Properties.EventType] = message.Type;

        var traceParent = activity?.Id ?? message.TraceParent;
        if (traceParent is not null)
        {
            serviceBusMessage.ApplicationProperties[MessagingConstants.Properties.TraceParent] = traceParent;
        }

        await senders.For(message.Destination).SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}

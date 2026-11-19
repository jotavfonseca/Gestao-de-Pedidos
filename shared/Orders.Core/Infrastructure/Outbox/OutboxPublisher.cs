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
/// Publica no Service Bus as mensagens pendentes do outbox. Usa
/// FOR UPDATE SKIP LOCKED para que várias instâncias não enviem a mesma
/// mensagem. Entrega at-least-once: duplicatas são absorvidas pelos consumidores.
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
                    // Esvazia o backlog em lotes antes de esperar o próximo tick.
                    while (await PublishBatchAsync(settings, stoppingToken) == settings.OutboxBatchSize)
                    {
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Falha ao varrer o outbox; nova tentativa no próximo ciclo.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal.
        }
    }

    /// <returns>Quantidade de mensagens publicadas com sucesso.</returns>
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
                    "Falha ao publicar a mensagem {MessageId} ({EventType}) do pedido {OrderId}; tentativa {Attempts}.",
                    message.Id, message.Type, message.CorrelationId, message.Attempts);

                if (message.Attempts >= settings.OutboxMaxAttempts)
                {
                    logger.LogError(
                        "Mensagem {MessageId} atingiu {MaxAttempts} tentativas e ficará parada no outbox para análise.",
                        message.Id, settings.OutboxMaxAttempts);
                }

                // Provavelmente o Service Bus está indisponível: para o lote e tenta no próximo ciclo.
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (published > 0)
        {
            logger.LogDebug("{Count} mensagem(ns) publicada(s) a partir do outbox.", published);
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

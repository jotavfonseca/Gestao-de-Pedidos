using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Core.Contracts;

namespace Orders.Core.Infrastructure.Messaging;

/// <summary>Resultado do processamento de uma mensagem.</summary>
public sealed record ConsumeResult(bool DeadLetter, string? Reason = null, string? Description = null)
{
    public static readonly ConsumeResult Complete = new(false);

    public static ConsumeResult ToDeadLetter(string reason, string description) => new(true, reason, description);
}

/// <summary>
/// Base para consumidores de fila: um escopo de DI por mensagem, span de
/// tracing ligado ao produtor e liquidação explícita (complete / dead-letter /
/// abandon em caso de exceção, para reentrega até o MaxDeliveryCount).
/// </summary>
public abstract class QueueConsumer(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    ILogger logger) : BackgroundService
{
    protected abstract string QueueName { get; }

    protected virtual int MaxConcurrentCalls => 1;

    protected abstract Task<ConsumeResult> HandleAsync(
        ServiceBusReceivedMessage message,
        IServiceProvider services,
        CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var processor = client.CreateProcessor(QueueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = MaxConcurrentCalls,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
        });

        processor.ProcessMessageAsync += OnMessageAsync;
        processor.ProcessErrorAsync += OnErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("Consumindo a fila {Queue}.", QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Encerramento normal.
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
        }
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;
        message.ApplicationProperties.TryGetValue(MessagingConstants.Properties.TraceParent, out var traceParent);

        using var activity = MessagingActivity.Start(
            $"process {QueueName}", ActivityKind.Consumer, traceParent as string,
            QueueName, message.MessageId, message.CorrelationId);

        using var logScope = logger.BeginScope(
            "Queue:{Queue} MessageId:{MessageId} OrderId:{OrderId} Delivery:{DeliveryCount}",
            QueueName, message.MessageId, message.CorrelationId, message.DeliveryCount);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var result = await HandleAsync(message, scope.ServiceProvider, args.CancellationToken);

            if (result.DeadLetter)
            {
                logger.LogWarning("Mensagem {MessageId} enviada para a dead-letter: {Reason} — {Description}",
                    message.MessageId, result.Reason, result.Description);
                await args.DeadLetterMessageAsync(message, result.Reason, result.Description, args.CancellationToken);
                activity?.SetStatus(ActivityStatusCode.Error, result.Reason);
                return;
            }

            await args.CompleteMessageAsync(message, args.CancellationToken);
        }
        catch (Exception ex) when (!args.CancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Erro ao processar a mensagem {MessageId} (entrega {DeliveryCount}); será reentregue.",
                message.MessageId, message.DeliveryCount);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await args.AbandonMessageAsync(message, cancellationToken: CancellationToken.None);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogWarning(args.Exception, "Erro no processador da fila {Queue} ({Source}).", QueueName, args.ErrorSource);
        return Task.CompletedTask;
    }
}

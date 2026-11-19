using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Core.Contracts;

namespace Orders.Core.Infrastructure.Messaging;

/// <summary>Outcome of handling a message.</summary>
public sealed record ConsumeResult(bool DeadLetter, string? Reason = null, string? Description = null)
{
    public static readonly ConsumeResult Complete = new(false);

    public static ConsumeResult ToDeadLetter(string reason, string description) => new(true, reason, description);
}

/// <summary>
/// Base class for queue consumers: one DI scope per message, a tracing span linked
/// to the producer, and explicit settlement (complete / dead-letter / abandon on
/// exceptions, so the message is redelivered up to MaxDeliveryCount).
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
        logger.LogInformation("Consuming queue {Queue}.", QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
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
                logger.LogWarning("Message {MessageId} dead-lettered: {Reason} — {Description}",
                    message.MessageId, result.Reason, result.Description);
                await args.DeadLetterMessageAsync(message, result.Reason, result.Description, args.CancellationToken);
                activity?.SetStatus(ActivityStatusCode.Error, result.Reason);
                return;
            }

            await args.CompleteMessageAsync(message, args.CancellationToken);
        }
        catch (Exception ex) when (!args.CancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Error handling message {MessageId} (delivery {DeliveryCount}); it will be redelivered.",
                message.MessageId, message.DeliveryCount);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await args.AbandonMessageAsync(message, cancellationToken: CancellationToken.None);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogWarning(args.Exception, "Processor error on queue {Queue} ({Source}).", QueueName, args.ErrorSource);
        return Task.CompletedTask;
    }
}

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Core.Contracts;

namespace Orders.Api.Health;

/// <summary>
/// Checks the queue with a peek (a data operation). Off-the-shelf checks use
/// management operations, which the Service Bus emulator does not support.
/// </summary>
internal sealed class ServiceBusHealthCheck(ServiceBusClient client) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);

            await using var receiver = client.CreateReceiver(MessagingConstants.Queues.OrderCreated);
            await receiver.PeekMessageAsync(cancellationToken: timeout.Token);

            return HealthCheckResult.Healthy($"Queue '{MessagingConstants.Queues.OrderCreated}' is reachable.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Service Bus is unavailable.", ex);
        }
    }
}

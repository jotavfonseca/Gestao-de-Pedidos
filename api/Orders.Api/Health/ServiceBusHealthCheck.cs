using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Core.Contracts;

namespace Orders.Api.Health;

/// <summary>
/// Verifica a fila com um peek (operação de dados). Os checks prontos usam
/// operações de administração, que o emulador do Service Bus não suporta.
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

            return HealthCheckResult.Healthy($"Fila '{MessagingConstants.Queues.OrderCreated}' acessível.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Service Bus indisponível.", ex);
        }
    }
}

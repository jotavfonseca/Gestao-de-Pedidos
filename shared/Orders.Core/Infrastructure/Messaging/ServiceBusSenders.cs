using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Orders.Core.Infrastructure.Messaging;

/// <summary>Um <see cref="ServiceBusSender"/> por fila, reaproveitado (thread-safe).</summary>
public sealed class ServiceBusSenders(ServiceBusClient client) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public ServiceBusSender For(string queueName) => _senders.GetOrAdd(queueName, client.CreateSender);

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
    }
}

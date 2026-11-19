using Microsoft.AspNetCore.SignalR;
using Orders.Core.Domain.Orders;

namespace Orders.Api.Realtime;

public interface IOrderNotifier
{
    Task OrderStatusChangedAsync(Guid orderId, OrderStatus status, DateTimeOffset changedAt, CancellationToken cancellationToken);
}

public sealed record OrderStatusChangedNotification(Guid OrderId, OrderStatus Status, DateTimeOffset ChangedAt);

/// <summary>
/// Envia a notificação a todos os clientes conectados. Falhas são só
/// registradas: o frontend tem fallback de polling (spec 06).
/// </summary>
internal sealed class SignalROrderNotifier(IHubContext<OrdersHub> hub, ILogger<SignalROrderNotifier> logger) : IOrderNotifier
{
    public async Task OrderStatusChangedAsync(Guid orderId, OrderStatus status, DateTimeOffset changedAt, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.All.SendAsync(
                OrdersHub.OrderStatusChangedEvent,
                new OrderStatusChangedNotification(orderId, status, changedAt),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Falha ao notificar a mudança de status do pedido {OrderId} via SignalR.", orderId);
        }
    }
}

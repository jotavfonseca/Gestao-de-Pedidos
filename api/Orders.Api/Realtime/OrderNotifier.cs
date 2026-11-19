using Microsoft.AspNetCore.SignalR;
using Orders.Core.Domain.Orders;

namespace Orders.Api.Realtime;

public interface IOrderNotifier
{
    Task OrderStatusChangedAsync(Guid orderId, OrderStatus status, DateTimeOffset changedAt, CancellationToken cancellationToken);
}

public sealed record OrderStatusChangedNotification(Guid OrderId, OrderStatus Status, DateTimeOffset ChangedAt);

/// <summary>
/// Sends the notification to every connected client. Failures are only logged:
/// the frontend has a polling fallback (spec 06).
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
            logger.LogWarning(ex, "Failed to notify the status change of order {OrderId} over SignalR.", orderId);
        }
    }
}

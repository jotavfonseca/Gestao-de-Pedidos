using Microsoft.AspNetCore.SignalR;

namespace Orders.Api.Realtime;

/// <summary>
/// Notification hub at /hubs/orders. Clients only receive the
/// <c>orderStatusChanged</c> event; there are no client-invoked methods.
/// </summary>
public sealed class OrdersHub : Hub
{
    public const string Path = "/hubs/orders";
    public const string OrderStatusChangedEvent = "orderStatusChanged";
}

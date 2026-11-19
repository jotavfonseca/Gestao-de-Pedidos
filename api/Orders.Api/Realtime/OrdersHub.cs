using Microsoft.AspNetCore.SignalR;

namespace Orders.Api.Realtime;

/// <summary>
/// Hub de notificações em /hubs/orders. Os clientes só recebem o evento
/// <c>orderStatusChanged</c>; não há métodos chamados pelo cliente.
/// </summary>
public sealed class OrdersHub : Hub
{
    public const string Path = "/hubs/orders";
    public const string OrderStatusChangedEvent = "orderStatusChanged";
}

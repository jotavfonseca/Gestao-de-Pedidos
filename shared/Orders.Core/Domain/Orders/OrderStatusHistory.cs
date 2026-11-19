namespace Orders.Core.Domain.Orders;

/// <summary>Entrada do histórico de status. Só é criada pelo agregado <see cref="Order"/>.</summary>
public sealed class OrderStatusHistory
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset Data { get; private set; }

    private OrderStatusHistory()
    {
    }

    internal OrderStatusHistory(Guid orderId, OrderStatus status, DateTimeOffset data)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Status = status;
        Data = data;
    }
}

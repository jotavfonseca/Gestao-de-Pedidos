namespace Orders.Core.Domain.Orders;

/// <summary>Status history entry. Only created by the <see cref="Order"/> aggregate.</summary>
public sealed class OrderStatusHistory
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }

    private OrderStatusHistory()
    {
    }

    internal OrderStatusHistory(Guid orderId, OrderStatus status, DateTimeOffset changedAt)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Status = status;
        ChangedAt = changedAt;
    }
}

using Orders.Core.Domain.Abstractions;
using Orders.Core.Domain.Orders.Events;

namespace Orders.Core.Domain.Orders;

/// <summary>
/// Order aggregate. Enforces the mandatory status sequence
/// (Pending → Processing → Completed), records the status history and
/// raises a domain event on every change.
/// </summary>
public sealed class Order : AggregateRoot
{
    public const int MaxTextLength = 100;

    private readonly List<OrderStatusHistory> _statusHistory = [];

    public Guid Id { get; private set; }
    public string Customer { get; private set; } = null!;
    public string Product { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private Order()
    {
    }

    public static Order Create(string customer, string product, Money amount, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(amount);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Customer = RequireText(customer, "customer"),
            Product = RequireText(product, "product"),
            Amount = amount,
            Status = OrderStatus.Pending,
            CreatedAt = now
        };

        order._statusHistory.Add(new OrderStatusHistory(order.Id, OrderStatus.Pending, now));
        order.Raise(new OrderCreated(order.Id, order.Customer, order.Product, amount.Value, now));
        return order;
    }

    public bool CanTransitionTo(OrderStatus next) =>
        (Status, next) is (OrderStatus.Pending, OrderStatus.Processing)
                       or (OrderStatus.Processing, OrderStatus.Completed);

    public void TransitionTo(OrderStatus next, DateTimeOffset now)
    {
        if (!CanTransitionTo(next))
        {
            throw new InvalidStatusTransitionException(Id, Status, next);
        }

        var previous = Status;
        Status = next;
        _statusHistory.Add(new OrderStatusHistory(Id, next, now));
        Raise(new OrderStatusChanged(Id, previous, next, now));
    }

    private static string RequireText(string? value, string field)
    {
        var trimmed = value?.Trim();
        var label = char.ToUpperInvariant(field[0]) + field[1..];

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainValidationException(field, $"{label} is required.");
        }

        if (trimmed.Length > MaxTextLength)
        {
            throw new DomainValidationException(field, $"{label} must be at most {MaxTextLength} characters.");
        }

        return trimmed;
    }
}

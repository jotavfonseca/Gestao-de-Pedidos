using Orders.Core.Domain.Orders;
using Orders.Core.Domain.Orders.Events;

namespace Orders.Tests.Domain;

public class OrderTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private static Order NewOrder() => Order.Create("Jane Doe", "Keyboard", Money.Of(100m), T0);

    [Fact]
    public void Create_starts_pending_with_history_and_OrderCreated_event()
    {
        var order = NewOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(T0, order.CreatedAt);
        var entry = Assert.Single(order.StatusHistory);
        Assert.Equal(OrderStatus.Pending, entry.Status);
        Assert.Equal(T0, entry.ChangedAt);

        var created = Assert.IsType<OrderCreated>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, created.OrderId);
        Assert.Equal(100m, created.Amount);
    }

    [Fact]
    public void Create_trims_text_fields()
    {
        var order = Order.Create("  Jane Doe  ", " Keyboard ", Money.Of(1m), T0);

        Assert.Equal("Jane Doe", order.Customer);
        Assert.Equal("Keyboard", order.Product);
    }

    [Theory]
    [InlineData("", "Keyboard", "customer")]
    [InlineData("   ", "Keyboard", "customer")]
    [InlineData("Jane Doe", "", "product")]
    public void Create_rejects_missing_text(string customer, string product, string field)
    {
        var ex = Assert.Throws<DomainValidationException>(() => Order.Create(customer, product, Money.Of(1m), T0));
        Assert.Equal(field, ex.Field);
    }

    [Fact]
    public void Create_rejects_text_longer_than_100()
    {
        var ex = Assert.Throws<DomainValidationException>(
            () => Order.Create(new string('a', 101), "Keyboard", Money.Of(1m), T0));
        Assert.Equal("customer", ex.Field);
        Assert.Equal("Customer must be at most 100 characters.", ex.Message);
    }

    [Fact]
    public void Follows_mandatory_sequence_recording_history_and_events()
    {
        var order = NewOrder();
        order.ClearDomainEvents();

        order.TransitionTo(OrderStatus.Processing, T0.AddSeconds(1));
        order.TransitionTo(OrderStatus.Completed, T0.AddSeconds(6));

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(
            [OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Completed],
            order.StatusHistory.Select(h => h.Status));

        var events = order.DomainEvents.Cast<OrderStatusChanged>().ToList();
        Assert.Equal(2, events.Count);
        Assert.Equal((OrderStatus.Pending, OrderStatus.Processing), (events[0].From, events[0].To));
        Assert.Equal((OrderStatus.Processing, OrderStatus.Completed), (events[1].From, events[1].To));
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Completed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing, OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing, OrderStatus.Processing)]
    [InlineData(OrderStatus.Completed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Completed, OrderStatus.Processing)]
    [InlineData(OrderStatus.Completed, OrderStatus.Completed)]
    public void Rejects_invalid_transitions_without_side_effects(OrderStatus current, OrderStatus next)
    {
        var order = OrderIn(current);
        var historyBefore = order.StatusHistory.Count;
        order.ClearDomainEvents();

        var ex = Assert.Throws<InvalidStatusTransitionException>(() => order.TransitionTo(next, T0.AddMinutes(1)));

        Assert.Equal((current, next), (ex.From, ex.To));
        Assert.Equal(current, order.Status);
        Assert.Equal(historyBefore, order.StatusHistory.Count);
        Assert.Empty(order.DomainEvents);
    }

    private static Order OrderIn(OrderStatus status)
    {
        var order = NewOrder();
        if (status >= OrderStatus.Processing) order.TransitionTo(OrderStatus.Processing, T0.AddSeconds(1));
        if (status == OrderStatus.Completed) order.TransitionTo(OrderStatus.Completed, T0.AddSeconds(6));
        return order;
    }
}

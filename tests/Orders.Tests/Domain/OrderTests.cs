using Orders.Core.Domain.Orders;
using Orders.Core.Domain.Orders.Events;

namespace Orders.Tests.Domain;

public class OrderTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private static Order NewOrder() => Order.Create("Ana", "Teclado", Money.Of(100m), T0);

    [Fact]
    public void Create_starts_pending_with_history_and_OrderCreated_event()
    {
        var order = NewOrder();

        Assert.Equal(OrderStatus.Pendente, order.Status);
        Assert.Equal(T0, order.DataCriacao);
        var entry = Assert.Single(order.Historico);
        Assert.Equal(OrderStatus.Pendente, entry.Status);
        Assert.Equal(T0, entry.Data);

        var created = Assert.IsType<OrderCreated>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, created.OrderId);
        Assert.Equal(100m, created.Valor);
    }

    [Fact]
    public void Create_trims_text_fields()
    {
        var order = Order.Create("  Ana  ", " Teclado ", Money.Of(1m), T0);

        Assert.Equal("Ana", order.Cliente);
        Assert.Equal("Teclado", order.Produto);
    }

    [Theory]
    [InlineData("", "Teclado", "cliente")]
    [InlineData("   ", "Teclado", "cliente")]
    [InlineData("Ana", "", "produto")]
    public void Create_rejects_missing_text(string cliente, string produto, string field)
    {
        var ex = Assert.Throws<DomainValidationException>(() => Order.Create(cliente, produto, Money.Of(1m), T0));
        Assert.Equal(field, ex.Field);
    }

    [Fact]
    public void Create_rejects_text_longer_than_100()
    {
        var ex = Assert.Throws<DomainValidationException>(
            () => Order.Create(new string('a', 101), "Teclado", Money.Of(1m), T0));
        Assert.Equal("cliente", ex.Field);
    }

    [Fact]
    public void Follows_mandatory_sequence_recording_history_and_events()
    {
        var order = NewOrder();
        order.ClearDomainEvents();

        order.TransitionTo(OrderStatus.Processando, T0.AddSeconds(1));
        order.TransitionTo(OrderStatus.Finalizado, T0.AddSeconds(6));

        Assert.Equal(OrderStatus.Finalizado, order.Status);
        Assert.Equal(
            [OrderStatus.Pendente, OrderStatus.Processando, OrderStatus.Finalizado],
            order.Historico.Select(h => h.Status));

        var events = order.DomainEvents.Cast<OrderStatusChanged>().ToList();
        Assert.Equal(2, events.Count);
        Assert.Equal((OrderStatus.Pendente, OrderStatus.Processando), (events[0].De, events[0].Para));
        Assert.Equal((OrderStatus.Processando, OrderStatus.Finalizado), (events[1].De, events[1].Para));
    }

    [Theory]
    [InlineData(OrderStatus.Pendente, OrderStatus.Finalizado)]
    [InlineData(OrderStatus.Pendente, OrderStatus.Pendente)]
    [InlineData(OrderStatus.Processando, OrderStatus.Pendente)]
    [InlineData(OrderStatus.Processando, OrderStatus.Processando)]
    [InlineData(OrderStatus.Finalizado, OrderStatus.Pendente)]
    [InlineData(OrderStatus.Finalizado, OrderStatus.Processando)]
    [InlineData(OrderStatus.Finalizado, OrderStatus.Finalizado)]
    public void Rejects_invalid_transitions_without_side_effects(OrderStatus current, OrderStatus next)
    {
        var order = OrderIn(current);
        var historyBefore = order.Historico.Count;
        order.ClearDomainEvents();

        var ex = Assert.Throws<InvalidStatusTransitionException>(() => order.TransitionTo(next, T0.AddMinutes(1)));

        Assert.Equal((current, next), (ex.From, ex.To));
        Assert.Equal(current, order.Status);
        Assert.Equal(historyBefore, order.Historico.Count);
        Assert.Empty(order.DomainEvents);
    }

    private static Order OrderIn(OrderStatus status)
    {
        var order = NewOrder();
        if (status >= OrderStatus.Processando) order.TransitionTo(OrderStatus.Processando, T0.AddSeconds(1));
        if (status == OrderStatus.Finalizado) order.TransitionTo(OrderStatus.Finalizado, T0.AddSeconds(6));
        return order;
    }
}

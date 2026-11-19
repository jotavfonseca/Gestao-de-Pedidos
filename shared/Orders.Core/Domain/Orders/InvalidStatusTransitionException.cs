using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Domain.Orders;

public sealed class InvalidStatusTransitionException(Guid orderId, OrderStatus from, OrderStatus to)
    : DomainException($"Transição de status inválida para o pedido {orderId}: {from} → {to}. Sequência permitida: Pendente → Processando → Finalizado.")
{
    public Guid OrderId { get; } = orderId;
    public OrderStatus From { get; } = from;
    public OrderStatus To { get; } = to;
}

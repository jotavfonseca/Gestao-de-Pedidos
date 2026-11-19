using Orders.Core.Domain.Abstractions;
using Orders.Core.Domain.Orders.Events;

namespace Orders.Core.Domain.Orders;

/// <summary>
/// Agregado Pedido. Garante a sequência obrigatória de status
/// (Pendente → Processando → Finalizado), registra o histórico e
/// emite eventos de domínio a cada mudança.
/// </summary>
public sealed class Order : AggregateRoot
{
    public const int MaxTextLength = 100;

    private readonly List<OrderStatusHistory> _historico = [];

    public Guid Id { get; private set; }
    public string Cliente { get; private set; } = null!;
    public string Produto { get; private set; } = null!;
    public Money Valor { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset DataCriacao { get; private set; }
    public IReadOnlyCollection<OrderStatusHistory> Historico => _historico.AsReadOnly();

    private Order()
    {
    }

    public static Order Create(string cliente, string produto, Money valor, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(valor);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Cliente = RequireText(cliente, "cliente"),
            Produto = RequireText(produto, "produto"),
            Valor = valor,
            Status = OrderStatus.Pendente,
            DataCriacao = agora
        };

        order._historico.Add(new OrderStatusHistory(order.Id, OrderStatus.Pendente, agora));
        order.Raise(new OrderCreated(order.Id, order.Cliente, order.Produto, valor.Amount, agora));
        return order;
    }

    public bool CanTransitionTo(OrderStatus next) =>
        (Status, next) is (OrderStatus.Pendente, OrderStatus.Processando)
                       or (OrderStatus.Processando, OrderStatus.Finalizado);

    public void TransitionTo(OrderStatus next, DateTimeOffset agora)
    {
        if (!CanTransitionTo(next))
        {
            throw new InvalidStatusTransitionException(Id, Status, next);
        }

        var previous = Status;
        Status = next;
        _historico.Add(new OrderStatusHistory(Id, next, agora));
        Raise(new OrderStatusChanged(Id, previous, next, agora));
    }

    private static string RequireText(string? value, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainValidationException(field, $"O campo '{field}' é obrigatório.");
        }

        if (trimmed.Length > MaxTextLength)
        {
            throw new DomainValidationException(field, $"O campo '{field}' deve ter no máximo {MaxTextLength} caracteres.");
        }

        return trimmed;
    }
}

using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Core.Application.Orders;

public sealed record CreateOrderCommand(string Cliente, string Produto, decimal Valor);

/// <summary>
/// Cria o pedido. A mensagem OrderCreated é gerada pelo agregado e gravada
/// no outbox pelo interceptor, na mesma transação.
/// </summary>
public sealed class CreateOrderHandler(OrderContext db, TimeProvider clock)
{
    public async Task<OrderDto> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = Order.Create(command.Cliente, command.Produto, Money.Of(command.Valor), clock.GetUtcNow());

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return OrderDto.From(order);
    }
}

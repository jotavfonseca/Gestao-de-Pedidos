using Microsoft.AspNetCore.Mvc;
using Orders.Api.Contracts;
using Orders.Api.Realtime;
using Orders.Core.Application.Orders;

namespace Orders.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(
    CreateOrderHandler createOrder,
    OrderQueries queries,
    IOrderNotifier notifier) : ControllerBase
{
    /// <summary>Creates an order (initial status Pending) and schedules the OrderCreated message.</summary>
    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await createOrder.HandleAsync(
            new CreateOrderCommand(request.Customer!, request.Product!, request.Amount!.Value),
            cancellationToken);

        await notifier.OrderStatusChangedAsync(order.Id, order.Status, order.CreatedAt, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, OrderResponse.From(order));
    }

    /// <summary>Lists orders, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<OrderResponse>> List(CancellationToken cancellationToken)
    {
        var orders = await queries.ListAsync(cancellationToken);
        return orders.Select(OrderResponse.From).ToList();
    }

    /// <summary>Order details, including the status history.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await queries.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : OrderResponse.From(order);
    }
}

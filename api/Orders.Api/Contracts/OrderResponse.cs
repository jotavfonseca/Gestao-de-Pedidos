using Orders.Core.Application.Orders;
using Orders.Core.Domain.Orders;

namespace Orders.Api.Contracts;

public sealed record OrderResponse(
    Guid Id,
    string Cliente,
    string Produto,
    decimal Valor,
    OrderStatus Status,
    DateTimeOffset DataCriacao,
    IReadOnlyList<StatusHistoryResponse> Historico)
{
    public static OrderResponse From(OrderDto order) => new(
        order.Id,
        order.Cliente,
        order.Produto,
        order.Valor,
        order.Status,
        order.DataCriacao,
        order.Historico.Select(h => new StatusHistoryResponse(h.Status, h.Data)).ToList());
}

public sealed record StatusHistoryResponse(OrderStatus Status, DateTimeOffset Data);

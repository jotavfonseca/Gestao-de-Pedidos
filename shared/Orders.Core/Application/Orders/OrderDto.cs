using Orders.Core.Domain.Orders;

namespace Orders.Core.Application.Orders;

/// <summary>Visão de leitura do pedido, usada pela API e pelos testes.</summary>
public sealed record OrderDto(
    Guid Id,
    string Cliente,
    string Produto,
    decimal Valor,
    OrderStatus Status,
    DateTimeOffset DataCriacao,
    IReadOnlyList<StatusHistoryDto> Historico)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.Cliente,
        order.Produto,
        order.Valor.Amount,
        order.Status,
        order.DataCriacao,
        order.Historico
            .OrderBy(h => h.Data)
            .ThenBy(h => h.Status)
            .Select(h => new StatusHistoryDto(h.Status, h.Data))
            .ToList());
}

public sealed record StatusHistoryDto(OrderStatus Status, DateTimeOffset Data);

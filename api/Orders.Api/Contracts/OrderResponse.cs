using Orders.Core.Application.Orders;
using Orders.Core.Domain.Orders;

namespace Orders.Api.Contracts;

public sealed record OrderResponse(
    Guid Id,
    string Customer,
    string Product,
    decimal Amount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<StatusHistoryResponse> StatusHistory)
{
    public static OrderResponse From(OrderDto order) => new(
        order.Id,
        order.Customer,
        order.Product,
        order.Amount,
        order.Status,
        order.CreatedAt,
        order.StatusHistory.Select(h => new StatusHistoryResponse(h.Status, h.ChangedAt)).ToList());
}

public sealed record StatusHistoryResponse(OrderStatus Status, DateTimeOffset ChangedAt);

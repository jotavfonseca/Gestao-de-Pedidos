using Orders.Core.Domain.Orders;

namespace Orders.Core.Application.Orders;

/// <summary>Read model of an order, used by the API and the tests.</summary>
public sealed record OrderDto(
    Guid Id,
    string Customer,
    string Product,
    decimal Amount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<StatusHistoryDto> StatusHistory)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.Customer,
        order.Product,
        order.Amount.Value,
        order.Status,
        order.CreatedAt,
        order.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Status)
            .Select(h => new StatusHistoryDto(h.Status, h.ChangedAt))
            .ToList());
}

public sealed record StatusHistoryDto(OrderStatus Status, DateTimeOffset ChangedAt);

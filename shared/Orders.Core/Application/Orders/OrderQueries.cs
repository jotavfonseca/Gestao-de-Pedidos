using Microsoft.EntityFrameworkCore;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Core.Application.Orders;

public sealed class OrderQueries(OrderContext db)
{
    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Historico)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : OrderDto.From(order);
    }

    public async Task<IReadOnlyList<OrderDto>> ListAsync(CancellationToken cancellationToken)
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Historico)
            .OrderByDescending(o => o.DataCriacao)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return orders.Select(OrderDto.From).ToList();
    }
}

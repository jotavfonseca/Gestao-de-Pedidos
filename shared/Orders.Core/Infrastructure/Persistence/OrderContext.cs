using Microsoft.EntityFrameworkCore;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Outbox;

namespace Orders.Core.Infrastructure.Persistence;

public sealed class OrderContext(DbContextOptions<OrderContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderContext).Assembly);
    }
}

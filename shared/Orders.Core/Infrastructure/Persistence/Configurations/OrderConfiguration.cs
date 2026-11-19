using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Core.Domain.Orders;

namespace Orders.Core.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Customer).IsRequired().HasMaxLength(Order.MaxTextLength);
        builder.Property(o => o.Product).IsRequired().HasMaxLength(Order.MaxTextLength);

        // Value object as a complex property: stored in the "Amount" column, and
        // queries such as Sum(o => o.Amount.Value) are translated to SQL.
        builder.ComplexProperty(o => o.Amount, money => money
            .Property(m => m.Value)
            .HasColumnName("Amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired());

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.HasIndex(o => o.CreatedAt);
        builder.HasIndex(o => o.Status);

        // Optimistic concurrency through PostgreSQL's xmin system column.
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.DomainEvents);
    }
}

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistory");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.Status).IsRequired().HasMaxLength(20).HasConversion<string>();
        builder.Property(h => h.ChangedAt).IsRequired();
        builder.HasIndex(h => new { h.OrderId, h.ChangedAt });
    }
}

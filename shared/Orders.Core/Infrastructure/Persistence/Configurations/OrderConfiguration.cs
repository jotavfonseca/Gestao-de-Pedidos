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

        builder.Property(o => o.Cliente).IsRequired().HasMaxLength(Order.MaxTextLength);
        builder.Property(o => o.Produto).IsRequired().HasMaxLength(Order.MaxTextLength);

        // Value object como complex property: a coluna continua "Valor" e
        // consultas como Sum(o => o.Valor.Amount) são traduzidas para SQL.
        builder.ComplexProperty(o => o.Valor, money => money
            .Property(m => m.Amount)
            .HasColumnName("Valor")
            .HasColumnType("numeric(18,2)")
            .IsRequired());

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(o => o.DataCriacao).IsRequired();
        builder.HasIndex(o => o.DataCriacao);
        builder.HasIndex(o => o.Status);

        // Concorrência otimista via coluna de sistema xmin do PostgreSQL.
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasMany(o => o.Historico)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Historico)
            .HasField("_historico")
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
        builder.Property(h => h.Data).IsRequired();
        builder.HasIndex(h => new { h.OrderId, h.Data });
    }
}

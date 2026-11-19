using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Core.Infrastructure.Outbox;

namespace Orders.Core.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Destination).IsRequired().HasMaxLength(100);
        builder.Property(m => m.CorrelationId).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.TraceParent).HasMaxLength(100);
        builder.Property(m => m.LastError).HasMaxLength(OutboxMessage.MaxErrorLength);

        // Consulta do publicador: pendentes em ordem de criação.
        builder.HasIndex(m => new { m.ProcessedAt, m.CreatedAt });
    }
}

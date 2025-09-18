using Microsoft.EntityFrameworkCore;
using OrderApi.Models;

namespace OrderApi.Data
{
    public class OrderContext : DbContext
    {
        public OrderContext(DbContextOptions<OrderContext> options) : base(options)
        {

        }
        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);  // Chave primária
                entity.Property(e => e.Cliente)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.Produto)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.Valor)
                      .IsRequired();
                entity.Property(e => e.Status)
                      .IsRequired();
                entity.Property(e => e.DataCriacao)
                      .IsRequired();
            });
        }
    }
}

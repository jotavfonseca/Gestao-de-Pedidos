using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orders.Core.Infrastructure.Persistence;

/// <summary>Permite rodar `dotnet ef` sem subir a API.</summary>
internal sealed class DesignTimeOrderContextFactory : IDesignTimeDbContextFactory<OrderContext>
{
    public OrderContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<OrderContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OrderContext(options);
    }
}

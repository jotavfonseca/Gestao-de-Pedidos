using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Tests.Infrastructure;

/// <summary>
/// Databases created by the Portuguese version must survive the RenameToEnglish
/// migration with their data intact (spec 15, criterion 3).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class MigrationTests(PostgresFixture postgres)
{
    [DockerFact]
    public async Task RenameToEnglish_keeps_data_and_translates_statuses()
    {
        var connectionString = await CreateEmptyDatabaseAsync();
        await using var db = new OrderContext(new DbContextOptionsBuilder<OrderContext>().UseNpgsql(connectionString).Options);
        var migrator = db.GetService<IMigrator>();

        // 1. Schema and data as the previous (Portuguese) version left them.
        await migrator.MigrateAsync("InitialCreate");
        var orderId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 11, 19, 13, 0, 0, TimeSpan.Zero);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Orders" ("Id", "Cliente", "Produto", "Valor", "Status", "DataCriacao")
            VALUES ({orderId}, 'Ana', 'Teclado', 349.90, 'Finalizado', {createdAt});
            """);
        foreach (var (status, seconds) in new[] { ("Pendente", 0), ("Processando", 1), ("Finalizado", 6) })
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "OrderStatusHistory" ("Id", "OrderId", "Status", "Data")
                VALUES ({Guid.NewGuid()}, {orderId}, {status}, {createdAt.AddSeconds(seconds)});
                """);
        }

        // 2. Upgrade to the current (English) schema.
        await migrator.MigrateAsync();

        // 3. The data reads back through the English model.
        var order = await db.Orders.AsNoTracking().Include(o => o.StatusHistory).SingleAsync(o => o.Id == orderId);
        Assert.Equal(("Ana", "Teclado", 349.90m, OrderStatus.Completed), (order.Customer, order.Product, order.Amount.Value, order.Status));
        Assert.Equal(createdAt, order.CreatedAt);
        Assert.Equal(
            [OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Completed],
            order.StatusHistory.OrderBy(h => h.ChangedAt).Select(h => h.Status));

        // 4. And the migration can be rolled back.
        await migrator.MigrateAsync("InitialCreate");
        var statuses = await db.Database.SqlQueryRaw<string>("""SELECT "Status" AS "Value" FROM "Orders" """).ToListAsync();
        Assert.Equal(["Finalizado"], statuses);
    }

    private async Task<string> CreateEmptyDatabaseAsync()
    {
        var name = $"migration_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = name }.ConnectionString;
    }
}

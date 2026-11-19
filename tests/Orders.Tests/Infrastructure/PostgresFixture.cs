using Microsoft.EntityFrameworkCore;
using Orders.Core.Infrastructure.Outbox;
using Orders.Core.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Orders.Tests.Infrastructure;

/// <summary>
/// A real PostgreSQL in a container (Testcontainers), migrated once per run.
/// Each test calls <see cref="ResetAsync"/> to start from a clean state.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Docker is unavailable: the container was not started.");

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        await _container.StartAsync();

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public OrderContext CreateContext(TimeProvider? clock = null) =>
        new(new DbContextOptionsBuilder<OrderContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new DomainEventsToOutboxInterceptor(clock ?? TimeProvider.System))
            .Options);

    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE "OrderStatusHistory", "OutboxMessages", "Orders" CASCADE""");
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

using Microsoft.EntityFrameworkCore;
using Orders.Core.Contracts;
using Orders.Core.Domain.Orders;

namespace Orders.Tests.Infrastructure;

[Collection(PostgresCollection.Name)]
public sealed class PersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    public Task InitializeAsync() => DockerAvailability.IsAvailable ? postgres.ResetAsync() : Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task Saving_aggregate_writes_domain_events_to_outbox_in_same_transaction()
    {
        var order = Order.Create("Jane Doe", "Keyboard", Money.Of(349.9m), T0);
        await using (var db = postgres.CreateContext())
        {
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            Assert.Empty(order.DomainEvents);
        }

        await using var read = postgres.CreateContext();
        var outbox = Assert.Single(await read.OutboxMessages.ToListAsync());
        Assert.Equal(MessagingConstants.EventTypes.OrderCreated, outbox.Type);
        Assert.Equal(MessagingConstants.Queues.OrderCreated, outbox.Destination);
        Assert.Equal(order.Id.ToString(), outbox.CorrelationId);
        Assert.Null(outbox.ProcessedAt);

        var payload = MessageSerializer.Deserialize<OrderCreatedMessage>(outbox.Payload)!;
        Assert.Equal((order.Id, "Jane Doe", "Keyboard", 349.90m), (payload.OrderId, payload.Customer, payload.Product, payload.Amount));
    }

    [DockerFact]
    public async Task Rolled_back_transaction_leaves_neither_order_nor_message()
    {
        await using (var db = postgres.CreateContext())
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Orders.Add(Order.Create("Jane Doe", "Keyboard", Money.Of(10m), T0));
            await db.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var read = postgres.CreateContext();
        Assert.Equal(0, await read.Orders.CountAsync());
        Assert.Equal(0, await read.OutboxMessages.CountAsync());
    }

    [DockerFact]
    public async Task Persists_status_as_text_and_amount_as_numeric()
    {
        await using (var db = postgres.CreateContext())
        {
            db.Orders.Add(Order.Create("Jane Doe", "Keyboard", Money.Of(10.5m), T0));
            await db.SaveChangesAsync();
        }

        await using var read = postgres.CreateContext();
        var row = await read.Database
            .SqlQueryRaw<StatusRow>("""SELECT "Status", "Amount" FROM "Orders" """)
            .SingleAsync();
        Assert.Equal(("Pending", 10.50m), (row.Status, row.Amount));
    }

    [DockerFact]
    public async Task Concurrent_updates_of_same_order_conflict()
    {
        var order = Order.Create("Jane Doe", "Keyboard", Money.Of(10m), T0);
        await using (var db = postgres.CreateContext())
        {
            db.Orders.Add(order);
            await db.SaveChangesAsync();
        }

        await using var first = postgres.CreateContext();
        await using var second = postgres.CreateContext();
        var a = await first.Orders.Include(o => o.StatusHistory).SingleAsync(o => o.Id == order.Id);
        var b = await second.Orders.Include(o => o.StatusHistory).SingleAsync(o => o.Id == order.Id);

        a.TransitionTo(OrderStatus.Processing, T0.AddSeconds(1));
        await first.SaveChangesAsync();

        b.TransitionTo(OrderStatus.Processing, T0.AddSeconds(1));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private sealed record StatusRow(string Status, decimal Amount);
}

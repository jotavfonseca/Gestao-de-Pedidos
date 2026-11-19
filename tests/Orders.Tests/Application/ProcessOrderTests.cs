using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orders.Core.Application.Orders;
using Orders.Core.Contracts;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Persistence;
using Orders.Tests.Infrastructure;

namespace Orders.Tests.Application;

/// <summary>Idempotência do processamento (spec 04) contra PostgreSQL real.</summary>
[Collection(PostgresCollection.Name)]
public sealed class ProcessOrderTests(PostgresFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => DockerAvailability.IsAvailable ? postgres.ResetAsync() : Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task Processes_pending_order_until_finalized()
    {
        var orderId = await SeedAsync();

        var outcome = await HandleAsync(orderId);

        Assert.Equal(ProcessOrderOutcome.Finalized, outcome);
        var order = await LoadAsync(orderId);
        Assert.Equal(OrderStatus.Finalizado, order.Status);
        Assert.Equal(
            [OrderStatus.Pendente, OrderStatus.Processando, OrderStatus.Finalizado],
            order.Historico.OrderBy(h => h.Data).Select(h => h.Status));
        Assert.Equal(2, await CountStatusChangedMessagesAsync());
    }

    [DockerFact]
    public async Task Duplicate_message_after_finalized_changes_nothing()
    {
        var orderId = await SeedAsync();
        await HandleAsync(orderId);

        var outcome = await HandleAsync(orderId);

        Assert.Equal(ProcessOrderOutcome.AlreadyFinalized, outcome);
        Assert.Equal(3, (await LoadAsync(orderId)).Historico.Count);
        Assert.Equal(2, await CountStatusChangedMessagesAsync());
    }

    [DockerFact]
    public async Task Redelivery_while_processing_resumes_without_duplicating_history()
    {
        var orderId = await SeedAsync();
        await using (var db = postgres.CreateContext())
        {
            var order = await db.Orders.Include(o => o.Historico).SingleAsync(o => o.Id == orderId);
            order.TransitionTo(OrderStatus.Processando, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var outcome = await HandleAsync(orderId);

        Assert.Equal(ProcessOrderOutcome.Finalized, outcome);
        var history = (await LoadAsync(orderId)).Historico.Select(h => h.Status).ToList();
        Assert.Equal(1, history.Count(s => s == OrderStatus.Processando));
        Assert.Equal(3, history.Count);
    }

    [DockerFact]
    public async Task Concurrent_deliveries_finalize_exactly_once()
    {
        var orderId = await SeedAsync();

        var outcomes = await Task.WhenAll(HandleAsync(orderId, delaySeconds: 1), HandleAsync(orderId, delaySeconds: 1));

        Assert.Single(outcomes, o => o == ProcessOrderOutcome.Finalized);
        Assert.Single(outcomes, o => o == ProcessOrderOutcome.AlreadyFinalized);
        var order = await LoadAsync(orderId);
        Assert.Equal(OrderStatus.Finalizado, order.Status);
        Assert.Equal(3, order.Historico.Count);
        Assert.Equal(2, await CountStatusChangedMessagesAsync());
    }

    [DockerFact]
    public async Task Unknown_order_is_reported_as_not_found()
    {
        Assert.Equal(ProcessOrderOutcome.OrderNotFound, await HandleAsync(Guid.NewGuid()));
    }

    private async Task<Guid> SeedAsync()
    {
        await using var db = postgres.CreateContext();
        var order = Order.Create("Ana", "Teclado", Money.Of(100m), DateTimeOffset.UtcNow);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    private async Task<ProcessOrderOutcome> HandleAsync(Guid orderId, int delaySeconds = 0)
    {
        await using var db = postgres.CreateContext();
        return await CreateHandler(db, delaySeconds).HandleAsync(orderId, CancellationToken.None);
    }

    internal static ProcessOrderHandler CreateHandler(OrderContext db, int delaySeconds = 0) => new(
        db,
        TimeProvider.System,
        Options.Create(new ProcessingOptions { DelaySeconds = delaySeconds }),
        NullLogger<ProcessOrderHandler>.Instance);

    private async Task<Order> LoadAsync(Guid orderId)
    {
        await using var db = postgres.CreateContext();
        return await db.Orders.AsNoTracking().Include(o => o.Historico).SingleAsync(o => o.Id == orderId);
    }

    private async Task<int> CountStatusChangedMessagesAsync()
    {
        await using var db = postgres.CreateContext();
        return await db.OutboxMessages.CountAsync(m => m.Type == MessagingConstants.EventTypes.OrderStatusChanged);
    }
}

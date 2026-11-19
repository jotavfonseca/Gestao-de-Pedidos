using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Core.Application.Orders;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";

    /// <summary>Time between Processing and Completed (challenge: 5 s).</summary>
    public int DelaySeconds { get; set; } = 5;
}

public enum ProcessOrderOutcome
{
    /// <summary>This run took the order all the way to Completed.</summary>
    Completed,

    /// <summary>The order was already completed: duplicate message, nothing to do.</summary>
    AlreadyCompleted,

    /// <summary>The order does not exist.</summary>
    OrderNotFound
}

/// <summary>
/// Processes an order idempotently. The aggregate's current state decides what to do,
/// and optimistic concurrency (xmin) prevents two deliveries of the same message from
/// performing the same transition:
/// <list type="bullet">
/// <item>Pending → Processing → (wait) → Completed</item>
/// <item>Processing (redelivery after a failure) → Completed</item>
/// <item>Completed → nothing</item>
/// </list>
/// </summary>
public sealed class ProcessOrderHandler(
    OrderContext db,
    TimeProvider clock,
    IOptions<ProcessingOptions> options,
    ILogger<ProcessOrderHandler> logger)
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<ProcessOrderOutcome> HandleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ProcessAsync(orderId, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                // Another delivery of the same message changed the order first: reload and re-evaluate.
                logger.LogInformation(
                    "Concurrency conflict on order {OrderId} (attempt {Attempt}); re-evaluating its state.",
                    orderId, attempt);
                db.ChangeTracker.Clear();
            }
        }
    }

    private async Task<ProcessOrderOutcome> ProcessAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.StatusHistory)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return ProcessOrderOutcome.OrderNotFound;
        }

        if (order.Status == OrderStatus.Completed)
        {
            logger.LogInformation("Order {OrderId} is already completed; message ignored (idempotency).", orderId);
            return ProcessOrderOutcome.AlreadyCompleted;
        }

        if (order.Status == OrderStatus.Pending)
        {
            order.TransitionTo(OrderStatus.Processing, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Order {OrderId} is processing.", orderId);
        }
        else
        {
            logger.LogInformation("Order {OrderId} was already processing; resuming.", orderId);
        }

        var delay = TimeSpan.FromSeconds(Math.Max(0, options.Value.DelaySeconds));
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, clock, cancellationToken);
        }

        order.TransitionTo(OrderStatus.Completed, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order {OrderId} completed.", orderId);

        return ProcessOrderOutcome.Completed;
    }
}

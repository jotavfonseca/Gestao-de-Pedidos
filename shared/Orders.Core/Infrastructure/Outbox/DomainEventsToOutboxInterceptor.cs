using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Infrastructure.Outbox;

/// <summary>
/// Before each SaveChanges, turns the domain events of tracked aggregates into
/// <see cref="OutboxMessage"/> rows. Aggregate and message are therefore written in
/// the same transaction, with no messaging code in the use cases.
/// </summary>
public sealed class DomainEventsToOutboxInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        EnqueueDomainEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnqueueDomainEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void EnqueueDomainEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
        {
            return;
        }

        // Activity.Current.Id is already in W3C "traceparent" format (the .NET default).
        var traceParent = Activity.Current?.Id;
        var now = clock.GetUtcNow();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                context.Set<OutboxMessage>().Add(DomainEventMapper.ToOutboxMessage(domainEvent, traceParent, now));
            }

            aggregate.ClearDomainEvents();
        }
    }
}

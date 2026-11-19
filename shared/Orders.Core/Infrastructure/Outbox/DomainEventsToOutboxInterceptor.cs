using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Orders.Core.Domain.Abstractions;

namespace Orders.Core.Infrastructure.Outbox;

/// <summary>
/// Antes de cada SaveChanges, converte os eventos de domínio dos agregados
/// rastreados em <see cref="OutboxMessage"/>. Assim agregado e mensagem são
/// gravados na mesma transação, sem código de mensageria nos casos de uso.
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

        // Activity.Current.Id já está no formato W3C "traceparent" (padrão no .NET).
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

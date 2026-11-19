namespace Orders.Core.Domain.Abstractions;

/// <summary>
/// Aggregate root: records domain events that the infrastructure turns into
/// outbox messages on save (see DomainEventsToOutboxInterceptor).
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

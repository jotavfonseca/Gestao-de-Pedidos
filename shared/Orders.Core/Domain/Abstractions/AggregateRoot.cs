namespace Orders.Core.Domain.Abstractions;

/// <summary>
/// Raiz de agregado: registra eventos de domínio que a infraestrutura
/// converte em mensagens de outbox ao salvar (ver DomainEventsToOutboxInterceptor).
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

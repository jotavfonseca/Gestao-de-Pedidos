namespace Orders.Core.Domain.Abstractions;

/// <summary>Violação de uma regra de negócio. Mapeada para HTTP na borda da API.</summary>
public abstract class DomainException(string message) : Exception(message);

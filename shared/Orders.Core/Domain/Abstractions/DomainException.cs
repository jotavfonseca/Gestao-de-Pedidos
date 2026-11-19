namespace Orders.Core.Domain.Abstractions;

/// <summary>A business rule violation. Mapped to HTTP at the API edge.</summary>
public abstract class DomainException(string message) : Exception(message);

namespace Orders.Core.Infrastructure.Outbox;

/// <summary>
/// Mensagem pendente de publicação. Gravada na mesma transação da mudança
/// de estado que a originou (Outbox Pattern).
/// </summary>
public sealed class OutboxMessage
{
    public const int MaxErrorLength = 2000;

    public Guid Id { get; private set; }

    /// <summary>EventType publicado (ex.: OrderCreated).</summary>
    public string Type { get; private set; } = null!;

    /// <summary>Fila de destino no Service Bus.</summary>
    public string Destination { get; private set; } = null!;

    public string Payload { get; private set; } = null!;
    public string CorrelationId { get; private set; } = null!;
    public string? TraceParent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create(
        string type,
        string destination,
        string correlationId,
        string payload,
        string? traceParent,
        DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Destination = destination,
        CorrelationId = correlationId,
        Payload = payload,
        TraceParent = traceParent,
        CreatedAt = createdAt
    };

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        Attempts++;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }
}

namespace Orders.Core.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>Desliga Service Bus, outbox publisher e consumidores (usado nos testes da API).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Intervalo de varredura do outbox.</summary>
    public int OutboxIntervalMilliseconds { get; set; } = 1000;

    /// <summary>Quantidade máxima de mensagens publicadas por varredura.</summary>
    public int OutboxBatchSize { get; set; } = 20;

    /// <summary>Após esse número de falhas a mensagem fica parada no outbox para análise.</summary>
    public int OutboxMaxAttempts { get; set; } = 10;
}

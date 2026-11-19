namespace Orders.Core.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>Turns off Service Bus, the outbox publisher and the consumers (used by the API tests).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Outbox polling interval.</summary>
    public int OutboxIntervalMilliseconds { get; set; } = 1000;

    /// <summary>Maximum number of messages published per scan.</summary>
    public int OutboxBatchSize { get; set; } = 20;

    /// <summary>After this many failures the message stays in the outbox for analysis.</summary>
    public int OutboxMaxAttempts { get; set; } = 10;
}

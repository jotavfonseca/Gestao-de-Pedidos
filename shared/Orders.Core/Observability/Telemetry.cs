using System.Diagnostics;

namespace Orders.Core.Observability;

public static class Telemetry
{
    public const string MessagingSourceName = "Orders.Messaging";

    /// <summary>Spans de publicação e consumo de mensagens (spec 08).</summary>
    public static readonly ActivitySource Messaging = new(MessagingSourceName);
}

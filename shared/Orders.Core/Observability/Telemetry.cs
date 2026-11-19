using System.Diagnostics;

namespace Orders.Core.Observability;

public static class Telemetry
{
    public const string MessagingSourceName = "Orders.Messaging";

    /// <summary>Message publish and consume spans (spec 08).</summary>
    public static readonly ActivitySource Messaging = new(MessagingSourceName);
}

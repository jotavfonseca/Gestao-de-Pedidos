using System.Diagnostics;
using Orders.Core.Observability;

namespace Orders.Core.Infrastructure.Messaging;

/// <summary>Cria spans de mensageria ligados ao traceparent recebido (spec 08).</summary>
internal static class MessagingActivity
{
    public static Activity? Start(string name, ActivityKind kind, string? traceParent, string queue, string messageId, string? orderId)
    {
        ActivityContext.TryParse(traceParent, null, out var parent);

        var activity = Telemetry.Messaging.StartActivity(name, kind, parent);
        activity?.SetTag("messaging.system", "servicebus");
        activity?.SetTag("messaging.destination.name", queue);
        activity?.SetTag("messaging.message.id", messageId);
        if (orderId is not null)
        {
            activity?.SetTag("order.id", orderId);
        }

        return activity;
    }
}

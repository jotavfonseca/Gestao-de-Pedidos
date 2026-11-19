namespace Orders.Core.Contracts;

public static class MessagingConstants
{
    public static class Queues
    {
        public const string OrderCreated = "order-created";
        public const string OrderStatusChanged = "order-status-changed";
    }

    public static class EventTypes
    {
        public const string OrderCreated = "OrderCreated";
        public const string OrderStatusChanged = "OrderStatusChanged";
    }

    public static class Properties
    {
        /// <summary>Application property required by the challenge.</summary>
        public const string EventType = "EventType";

        /// <summary>W3C trace context for end-to-end tracing.</summary>
        public const string TraceParent = "traceparent";
    }
}

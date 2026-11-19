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
        /// <summary>Application property exigida pelo desafio.</summary>
        public const string EventType = "EventType";

        /// <summary>Contexto W3C para tracing ponta a ponta.</summary>
        public const string TraceParent = "traceparent";
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orders.Core.Contracts;

/// <summary>
/// Contracts published to Service Bus. They are stable and independent from the
/// domain events, so the domain can evolve without breaking consumers.
/// </summary>
public sealed record OrderCreatedMessage(
    Guid OrderId,
    string Customer,
    string Product,
    decimal Amount,
    DateTimeOffset CreatedAt);

public sealed record OrderStatusChangedMessage(
    Guid OrderId,
    string PreviousStatus,
    string Status,
    DateTimeOffset ChangedAt);

public static class MessageSerializer
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T message) => JsonSerializer.Serialize(message, Options);

    public static T? Deserialize<T>(BinaryData body) => body.ToObjectFromJson<T>(Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}

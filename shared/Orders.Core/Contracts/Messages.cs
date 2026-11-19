using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orders.Core.Contracts;

/// <summary>
/// Contratos publicados no Service Bus. São estáveis e independentes dos
/// eventos de domínio: o domínio pode evoluir sem quebrar consumidores.
/// </summary>
public sealed record OrderCreatedMessage(
    Guid OrderId,
    string Cliente,
    string Produto,
    decimal Valor,
    DateTimeOffset DataCriacao);

public sealed record OrderStatusChangedMessage(
    Guid OrderId,
    string StatusAnterior,
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

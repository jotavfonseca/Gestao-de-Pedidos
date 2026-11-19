using System.Text.Json;
using System.Text.Json.Serialization;
using Orders.Api.Contracts;
using Orders.Core.Application.Orders;
using Orders.Core.Domain.Orders;
using Orders.Core.Domain.Orders.Events;
using Orders.Core.Infrastructure.Outbox;

namespace Orders.Tests.Golden;

/// <summary>
/// Golden tests dos contratos públicos: o JSON da API e o corpo das
/// mensagens no Service Bus. Qualquer mudança de formato quebra o teste.
/// </summary>
public class ContractGoldenTests
{
    private static readonly Guid OrderId = Guid.Parse("4f0c6c8e-2d3b-4a8e-9a51-1f2e3d4c5b6a");
    private static readonly DateTimeOffset T0 = new(2026, 11, 19, 13, 0, 0, TimeSpan.Zero);

    // Mesmas opções que o ASP.NET Core usa nos controllers (Web + enum como texto).
    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Order_response()
    {
        var dto = new OrderDto(OrderId, "Ana Souza", "Teclado mecânico", 349.90m, OrderStatus.Finalizado, T0,
        [
            new StatusHistoryDto(OrderStatus.Pendente, T0),
            new StatusHistoryDto(OrderStatus.Processando, T0.AddSeconds(1)),
            new StatusHistoryDto(OrderStatus.Finalizado, T0.AddSeconds(6))
        ]);

        GoldenFile.Assert("order-response", JsonSerializer.Serialize(OrderResponse.From(dto), ApiJson));
    }

    [Fact]
    public void Order_created_message()
    {
        var message = DomainEventMapper.ToOutboxMessage(
            new OrderCreated(OrderId, "Ana Souza", "Teclado mecânico", 349.90m, T0), traceParent: null, T0);

        Assert.Equal(("OrderCreated", "order-created", OrderId.ToString()),
            (message.Type, message.Destination, message.CorrelationId));
        GoldenFile.Assert("order-created-message", message.Payload);
    }

    [Fact]
    public void Order_status_changed_message()
    {
        var message = DomainEventMapper.ToOutboxMessage(
            new OrderStatusChanged(OrderId, OrderStatus.Processando, OrderStatus.Finalizado, T0.AddSeconds(6)),
            traceParent: null, T0);

        Assert.Equal(("OrderStatusChanged", "order-status-changed", OrderId.ToString()),
            (message.Type, message.Destination, message.CorrelationId));
        GoldenFile.Assert("order-status-changed-message", message.Payload);
    }
}

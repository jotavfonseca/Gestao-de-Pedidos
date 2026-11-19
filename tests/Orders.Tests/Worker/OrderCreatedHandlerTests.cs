using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.Core.Contracts;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Messaging;
using Orders.Tests.Application;
using Orders.Tests.Infrastructure;
using Orders.Worker;

namespace Orders.Tests.Worker;

/// <summary>Validação da mensagem e liquidação (complete / dead-letter) no worker.</summary>
[Collection(PostgresCollection.Name)]
public sealed class OrderCreatedHandlerTests(PostgresFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => DockerAvailability.IsAvailable ? postgres.ResetAsync() : Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task Valid_message_is_processed_and_completed()
    {
        Guid orderId;
        await using (var db = postgres.CreateContext())
        {
            var order = Order.Create("Ana", "Teclado", Money.Of(10m), DateTimeOffset.UtcNow);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        var result = await HandleAsync(Message(MessagingConstants.EventTypes.OrderCreated, Body(orderId), orderId));

        Assert.False(result.DeadLetter);
    }

    [DockerFact]
    public async Task Unexpected_event_type_goes_to_dead_letter()
    {
        var result = await HandleAsync(Message("SomethingElse", Body(Guid.NewGuid())));

        Assert.True(result.DeadLetter);
        Assert.Equal("UnexpectedEventType", result.Reason);
    }

    [DockerFact]
    public async Task Invalid_body_goes_to_dead_letter()
    {
        var result = await HandleAsync(Message(MessagingConstants.EventTypes.OrderCreated, "{ not json"));

        Assert.True(result.DeadLetter);
        Assert.Equal("InvalidBody", result.Reason);
    }

    [DockerFact]
    public async Task Unknown_order_goes_to_dead_letter()
    {
        var result = await HandleAsync(Message(MessagingConstants.EventTypes.OrderCreated, Body(Guid.NewGuid())));

        Assert.True(result.DeadLetter);
        Assert.Equal("OrderNotFound", result.Reason);
    }

    private async Task<ConsumeResult> HandleAsync(ServiceBusReceivedMessage message)
    {
        await using var db = postgres.CreateContext();
        var handler = new OrderCreatedHandler(ProcessOrderTests.CreateHandler(db), NullLogger<OrderCreatedHandler>.Instance);
        return await handler.HandleAsync(message, CancellationToken.None);
    }

    private static string Body(Guid orderId) =>
        MessageSerializer.Serialize(new OrderCreatedMessage(orderId, "Ana", "Teclado", 10m, DateTimeOffset.UtcNow));

    private static ServiceBusReceivedMessage Message(string eventType, string body, Guid? correlationId = null) =>
        ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(body),
            messageId: Guid.NewGuid().ToString(),
            correlationId: correlationId?.ToString(),
            properties: new Dictionary<string, object> { [MessagingConstants.Properties.EventType] = eventType });
}

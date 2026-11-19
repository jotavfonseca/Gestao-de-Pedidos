using System.Text.Json;
using Orders.Api.Ai;
using Orders.Core.Domain.Orders;
using Orders.Tests.Infrastructure;

namespace Orders.Tests.Ai;

/// <summary>Ferramentas do módulo IA (spec 10) com dados conhecidos.</summary>
[Collection(PostgresCollection.Name)]
public sealed class OrderAnalyticsToolsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    // 19/11/2026 10:00 em São Paulo (UTC-3)
    private static readonly DateTimeOffset Today = new(2026, 11, 19, 13, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable) return;
        await postgres.ResetAsync();

        await using var db = postgres.CreateContext();
        // Finalizado hoje em 6 s; finalizado hoje em 10 s; pendente hoje;
        // pendente às 23:00 de ontem em São Paulo (02:00 UTC de hoje).
        db.Orders.AddRange(
            Finalized(Order.Create("A", "P1", Money.Of(100m), Today), seconds: 6),
            Finalized(Order.Create("B", "P2", Money.Of(50.5m), Today.AddMinutes(1)), seconds: 10),
            Order.Create("C", "P3", Money.Of(20m), Today.AddMinutes(2)),
            Order.Create("D", "P4", Money.Of(7m), new DateTimeOffset(2026, 11, 19, 2, 0, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task Counts_by_status()
    {
        var result = await RunAsync(OrderAnalyticsTools.CountOrders, new { status = "Pendente" });
        Assert.Equal(2, result.GetProperty("quantidade").GetInt32());
    }

    [DockerFact]
    public async Task Dates_without_offset_use_sao_paulo_time_zone()
    {
        var result = await RunAsync(OrderAnalyticsTools.CountOrders, new { from = "2026-11-19", to = "2026-11-20" });

        // O pedido das 23:00 de 18/11 (horário de Brasília) fica de fora.
        Assert.Equal(3, result.GetProperty("quantidade").GetInt32());
    }

    [DockerFact]
    public async Task Sums_values_of_finalized_orders()
    {
        var result = await RunAsync(OrderAnalyticsTools.SumOrderValues, new { status = "Finalizado" });

        Assert.Equal(150.5m, result.GetProperty("valorTotal").GetDecimal());
        Assert.Equal(75.25m, result.GetProperty("ticketMedio").GetDecimal());
    }

    [DockerFact]
    public async Task Average_processing_time_uses_status_history()
    {
        var result = await RunAsync(OrderAnalyticsTools.AverageProcessingTime, new { });

        Assert.Equal(2, result.GetProperty("pedidosConsiderados").GetInt32());
        Assert.Equal(8, result.GetProperty("mediaSegundos").GetDouble());
        Assert.Equal(6, result.GetProperty("minimoSegundos").GetDouble());
        Assert.Equal(10, result.GetProperty("maximoSegundos").GetDouble());
    }

    [DockerFact]
    public async Task Groups_by_status()
    {
        var result = await RunAsync(OrderAnalyticsTools.OrdersByStatus, new { });

        var counts = result.GetProperty("porStatus").EnumerateArray()
            .ToDictionary(e => e.GetProperty("status").GetString()!, e => e.GetProperty("quantidade").GetInt32());
        Assert.Equal(new Dictionary<string, int> { ["Pendente"] = 2, ["Processando"] = 0, ["Finalizado"] = 2 }, counts);
    }

    [DockerFact]
    public async Task Rejects_invalid_parameters()
    {
        await Assert.ThrowsAsync<ToolInputException>(() => RunAsync(OrderAnalyticsTools.CountOrders, new { status = "Cancelado" }));
        await Assert.ThrowsAsync<ToolInputException>(() => RunAsync(OrderAnalyticsTools.ListRecentOrders, new { limit = 500 }));
        await Assert.ThrowsAsync<ToolInputException>(() => RunAsync(OrderAnalyticsTools.CountOrders, new { from = "ontem" }));
        await Assert.ThrowsAsync<ToolInputException>(() => RunAsync("drop_table", new { }));
    }

    private async Task<JsonElement> RunAsync(string tool, object input)
    {
        await using var db = postgres.CreateContext();
        var tools = new OrderAnalyticsTools(db, SaoPaulo);
        var arguments = JsonSerializer.SerializeToElement(input).EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        return JsonDocument.Parse(await tools.ExecuteAsync(tool, arguments, CancellationToken.None)).RootElement;
    }

    private static Order Finalized(Order order, int seconds)
    {
        order.TransitionTo(OrderStatus.Processando, order.DataCriacao.AddSeconds(1));
        order.TransitionTo(OrderStatus.Finalizado, order.DataCriacao.AddSeconds(seconds));
        return order;
    }
}

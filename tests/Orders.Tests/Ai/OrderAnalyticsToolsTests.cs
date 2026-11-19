using System.Text.Json;
using Orders.Api.Ai;
using Orders.Core.Domain.Orders;
using Orders.Tests.Infrastructure;

namespace Orders.Tests.Ai;

/// <summary>AI module tools (spec 10) with known data.</summary>
[Collection(PostgresCollection.Name)]
public sealed class OrderAnalyticsToolsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    // Nov 19, 2026 10:00 in São Paulo (UTC-3)
    private static readonly DateTimeOffset Today = new(2026, 11, 19, 13, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable) return;
        await postgres.ResetAsync();

        await using var db = postgres.CreateContext();
        // Completed today in 6 s; completed today in 10 s; pending today;
        // pending at 23:00 yesterday in São Paulo (02:00 UTC today).
        db.Orders.AddRange(
            Completed(Order.Create("A", "P1", Money.Of(100m), Today), seconds: 6),
            Completed(Order.Create("B", "P2", Money.Of(50.5m), Today.AddMinutes(1)), seconds: 10),
            Order.Create("C", "P3", Money.Of(20m), Today.AddMinutes(2)),
            Order.Create("D", "P4", Money.Of(7m), new DateTimeOffset(2026, 11, 19, 2, 0, 0, TimeSpan.Zero)));
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DockerFact]
    public async Task Counts_by_status()
    {
        var result = await RunAsync(OrderAnalyticsTools.CountOrders, new { status = "Pending" });
        Assert.Equal(2, result.GetProperty("count").GetInt32());
    }

    [DockerFact]
    public async Task Dates_without_offset_use_the_configured_time_zone()
    {
        var result = await RunAsync(OrderAnalyticsTools.CountOrders, new { from = "2026-11-19", to = "2026-11-20" });

        // The order created at 23:00 on Nov 18 (São Paulo time) is left out.
        Assert.Equal(3, result.GetProperty("count").GetInt32());
    }

    [DockerFact]
    public async Task Sums_amounts_of_completed_orders()
    {
        var result = await RunAsync(OrderAnalyticsTools.SumOrderAmounts, new { status = "Completed" });

        Assert.Equal(150.5m, result.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(75.25m, result.GetProperty("averageAmount").GetDecimal());
    }

    [DockerFact]
    public async Task Average_processing_time_uses_status_history()
    {
        var result = await RunAsync(OrderAnalyticsTools.AverageProcessingTime, new { });

        Assert.Equal(2, result.GetProperty("ordersConsidered").GetInt32());
        Assert.Equal(8, result.GetProperty("averageSeconds").GetDouble());
        Assert.Equal(6, result.GetProperty("minSeconds").GetDouble());
        Assert.Equal(10, result.GetProperty("maxSeconds").GetDouble());
    }

    [DockerFact]
    public async Task Groups_by_status()
    {
        var result = await RunAsync(OrderAnalyticsTools.OrdersByStatus, new { });

        var counts = result.GetProperty("byStatus").EnumerateArray()
            .ToDictionary(e => e.GetProperty("status").GetString()!, e => e.GetProperty("count").GetInt32());
        Assert.Equal(new Dictionary<string, int> { ["Pending"] = 2, ["Processing"] = 0, ["Completed"] = 2 }, counts);
    }

    [DockerFact]
    public async Task Rejects_invalid_parameters()
    {
        await Assert.ThrowsAsync<ToolInputException>(() => RunAsync(OrderAnalyticsTools.CountOrders, new { status = "Cancelled" }));
        await Assert.ThrowsAsync<ToolInputException>(() => RunAsync(OrderAnalyticsTools.ListRecentOrders, new { limit = 500 }));
        await Assert.ThrowsAsync<ToolInputException>(() => RunAsync(OrderAnalyticsTools.CountOrders, new { from = "yesterday" }));
        await Assert.ThrowsAsync<ToolInputException>(() => RunAsync("drop_table", new { }));
    }

    private async Task<JsonElement> RunAsync(string tool, object input)
    {
        await using var db = postgres.CreateContext();
        var tools = new OrderAnalyticsTools(db, SaoPaulo);
        var arguments = JsonSerializer.SerializeToElement(input).EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        return JsonDocument.Parse(await tools.ExecuteAsync(tool, arguments, CancellationToken.None)).RootElement;
    }

    private static Order Completed(Order order, int seconds)
    {
        order.TransitionTo(OrderStatus.Processing, order.CreatedAt.AddSeconds(1));
        order.TransitionTo(OrderStatus.Completed, order.CreatedAt.AddSeconds(seconds));
        return order;
    }
}

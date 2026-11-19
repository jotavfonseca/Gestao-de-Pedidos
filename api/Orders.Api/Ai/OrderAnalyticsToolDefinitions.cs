using System.Text.Json;
using Anthropic.Models.Beta.Messages;

namespace Orders.Api.Ai;

/// <summary>Schemas of the tools exposed to Claude.</summary>
internal static class OrderAnalyticsToolDefinitions
{
    private static readonly JsonElement Status = Schema(new
    {
        type = "string",
        @enum = new[] { "Pending", "Processing", "Completed" },
        description = "Filters by status (optional)."
    });

    private static readonly JsonElement From = Schema(new
    {
        type = "string",
        description = "Start of the period, inclusive, on the creation date. ISO-8601: 2026-11-01 or 2026-11-01T08:00:00 (no offset = configured time zone)."
    });

    private static readonly JsonElement To = Schema(new
    {
        type = "string",
        description = "End of the period, exclusive, on the creation date. Same format as 'from'."
    });

    public static readonly List<BetaToolUnion> All =
    [
        Tool(OrderAnalyticsTools.CountOrders,
            "Counts orders, with optional status and creation-period filters.",
            new() { ["status"] = Status, ["from"] = From, ["to"] = To }),

        Tool(OrderAnalyticsTools.SumOrderAmounts,
            "Sums order amounts (USD) and computes the average order value, with optional status and creation-period filters.",
            new() { ["status"] = Status, ["from"] = From, ["to"] = To }),

        Tool(OrderAnalyticsTools.AverageProcessingTime,
            "Average, minimum and maximum time between creation (Pending) and completion (Completed) of completed orders, " +
            "optionally only for orders created in the period.",
            new() { ["from"] = From, ["to"] = To }),

        Tool(OrderAnalyticsTools.OrdersByStatus,
            "Number of orders and total amount grouped by status, optionally only for orders created in the period.",
            new() { ["from"] = From, ["to"] = To }),

        Tool(OrderAnalyticsTools.ListRecentOrders,
            "Lists the most recent orders (id, customer, product, amount, status, creation date).",
            new()
            {
                ["limit"] = Schema(new { type = "integer", minimum = 1, maximum = 20, description = "How many (default 10)." }),
                ["status"] = Status,
                ["from"] = From,
                ["to"] = To
            })
    ];

    private static BetaToolUnion Tool(string name, string description, Dictionary<string, JsonElement> properties) =>
        new BetaTool
        {
            Name = name,
            Description = description,
            InputSchema = new() { Properties = properties }
        };

    private static JsonElement Schema(object value) => JsonSerializer.SerializeToElement(value);
}

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Api.Ai;

/// <summary>Invalid parameter sent by the LLM; returned to it as an error tool_result.</summary>
public sealed class ToolInputException(string message) : Exception(message);

/// <summary>
/// Tools the LLM can call. All of them are read-only, with validated parameters
/// and fixed queries — the LLM never writes SQL.
/// </summary>
public sealed partial class OrderAnalyticsTools(OrderContext db, TimeZoneInfo timeZone)
{
    public const string CountOrders = "count_orders";
    public const string SumOrderAmounts = "sum_order_amounts";
    public const string AverageProcessingTime = "avg_processing_time";
    public const string OrdersByStatus = "orders_by_status";
    public const string ListRecentOrders = "list_recent_orders";

    private const int MaxListLimit = 20;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> ExecuteAsync(string name, IReadOnlyDictionary<string, JsonElement> input, CancellationToken cancellationToken)
    {
        object result = name switch
        {
            CountOrders => await CountAsync(input, cancellationToken),
            SumOrderAmounts => await SumAsync(input, cancellationToken),
            AverageProcessingTime => await AverageProcessingTimeAsync(input, cancellationToken),
            OrdersByStatus => await ByStatusAsync(input, cancellationToken),
            ListRecentOrders => await ListRecentAsync(input, cancellationToken),
            _ => throw new ToolInputException($"Unknown tool: {name}.")
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private async Task<object> CountAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: true);
        var count = await Apply(filter).CountAsync(ct);
        return new { filter = filter.Describe(), count };
    }

    private async Task<object> SumAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: true);
        var query = Apply(filter);
        var count = await query.CountAsync(ct);
        var total = count == 0 ? 0m : await query.SumAsync(o => o.Amount.Value, ct);
        return new
        {
            filter = filter.Describe(),
            count,
            totalAmount = total,
            averageAmount = count == 0 ? 0m : Math.Round(total / count, 2)
        };
    }

    private async Task<object> AverageProcessingTimeAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: false) with { Status = OrderStatus.Completed };

        // From the Pending entry (creation) to the Completed entry in the status history.
        var spans = await Apply(filter)
            .Select(o => new
            {
                Start = o.StatusHistory.Where(h => h.Status == OrderStatus.Pending).Min(h => h.ChangedAt),
                End = o.StatusHistory.Where(h => h.Status == OrderStatus.Completed).Max(h => h.ChangedAt)
            })
            .ToListAsync(ct);

        var seconds = spans.Select(s => (s.End - s.Start).TotalSeconds).ToList();
        return new
        {
            filter = filter.Describe(),
            measures = "Time from order creation (Pending) to completion (Completed).",
            ordersConsidered = seconds.Count,
            averageSeconds = seconds.Count == 0 ? (double?)null : Math.Round(seconds.Average(), 2),
            minSeconds = seconds.Count == 0 ? (double?)null : Math.Round(seconds.Min(), 2),
            maxSeconds = seconds.Count == 0 ? (double?)null : Math.Round(seconds.Max(), 2)
        };
    }

    private async Task<object> ByStatusAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: false);
        var rows = await Apply(filter)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), TotalAmount = g.Sum(o => o.Amount.Value) })
            .ToListAsync(ct);

        return new
        {
            filter = filter.Describe(),
            byStatus = Enum.GetValues<OrderStatus>().Select(status =>
            {
                var row = rows.SingleOrDefault(r => r.Status == status);
                return new { status = status.ToString(), count = row?.Count ?? 0, totalAmount = row?.TotalAmount ?? 0m };
            })
        };
    }

    private async Task<object> ListRecentAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: true);
        var limit = 10;
        if (input.TryGetValue("limit", out var rawLimit) && rawLimit.ValueKind != JsonValueKind.Null)
        {
            if (rawLimit.ValueKind != JsonValueKind.Number || !rawLimit.TryGetInt32(out limit) || limit is < 1 or > MaxListLimit)
            {
                throw new ToolInputException($"'limit' must be an integer between 1 and {MaxListLimit}.");
            }
        }

        var orders = await Apply(filter)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .Select(o => new { o.Id, o.Customer, o.Product, Amount = o.Amount.Value, o.Status, o.CreatedAt })
            .ToListAsync(ct);

        return new
        {
            filter = filter.Describe(),
            orders = orders.Select(o => new
            {
                id = o.Id,
                customer = o.Customer,
                product = o.Product,
                amount = o.Amount,
                status = o.Status.ToString(),
                createdAt = TimeZoneInfo.ConvertTime(o.CreatedAt, timeZone).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            })
        };
    }

    private IQueryable<Order> Apply(OrderFilter filter)
    {
        var query = db.Orders.AsNoTracking();
        if (filter.Status is { } status)
        {
            query = query.Where(o => o.Status == status);
        }

        if (filter.From is { } from)
        {
            query = query.Where(o => o.CreatedAt >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(o => o.CreatedAt < to);
        }

        return query;
    }

    private OrderFilter ReadFilter(IReadOnlyDictionary<string, JsonElement> input, bool allowStatus)
    {
        OrderStatus? status = null;
        if (allowStatus && input.TryGetValue("status", out var rawStatus) && rawStatus.ValueKind != JsonValueKind.Null)
        {
            if (rawStatus.ValueKind != JsonValueKind.String || !Enum.TryParse<OrderStatus>(rawStatus.GetString(), ignoreCase: true, out var parsed))
            {
                throw new ToolInputException("'status' must be Pending, Processing or Completed.");
            }

            status = parsed;
        }

        var from = ReadDate(input, "from");
        var to = ReadDate(input, "to");
        if (from is not null && to is not null && from >= to)
        {
            throw new ToolInputException("'from' must be earlier than 'to'.");
        }

        return new OrderFilter(status, from, to);
    }

    /// <summary>Accepts a date (yyyy-MM-dd) or an ISO-8601 date-time; without an offset, the configured time zone is used.</summary>
    private DateTimeOffset? ReadDate(IReadOnlyDictionary<string, JsonElement> input, string key)
    {
        if (!input.TryGetValue(key, out var raw) || raw.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var text = raw.ValueKind == JsonValueKind.String ? raw.GetString() : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ToolInputException($"'{key}' must be an ISO-8601 date.");
        }

        if (ExplicitOffset().IsMatch(text)
            && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset))
        {
            return withOffset.ToUniversalTime();
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            return new DateTimeOffset(unspecified, timeZone.GetUtcOffset(unspecified)).ToUniversalTime();
        }

        throw new ToolInputException($"'{key}' must be an ISO-8601 date (e.g. 2026-11-01 or 2026-11-01T08:00:00).");
    }

    [GeneratedRegex(@"(Z|[+-]\d{2}:?\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitOffset();

    private sealed record OrderFilter(OrderStatus? Status, DateTimeOffset? From, DateTimeOffset? To)
    {
        public object Describe() => new
        {
            status = Status?.ToString(),
            from = From?.ToString("O", CultureInfo.InvariantCulture),
            to = To?.ToString("O", CultureInfo.InvariantCulture)
        };
    }
}

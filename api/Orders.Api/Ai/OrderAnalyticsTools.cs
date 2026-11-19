using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Api.Ai;

/// <summary>Parâmetro inválido enviado pela LLM; devolvido a ela como tool_result de erro.</summary>
public sealed class ToolInputException(string message) : Exception(message);

/// <summary>
/// Ferramentas que a LLM pode chamar. Todas são somente leitura, com
/// parâmetros validados e consultas fixas — a LLM nunca escreve SQL.
/// </summary>
public sealed partial class OrderAnalyticsTools(OrderContext db, TimeZoneInfo timeZone)
{
    public const string CountOrders = "count_orders";
    public const string SumOrderValues = "sum_order_values";
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
            SumOrderValues => await SumAsync(input, cancellationToken),
            AverageProcessingTime => await AverageProcessingTimeAsync(input, cancellationToken),
            OrdersByStatus => await ByStatusAsync(input, cancellationToken),
            ListRecentOrders => await ListRecentAsync(input, cancellationToken),
            _ => throw new ToolInputException($"Ferramenta desconhecida: {name}.")
        };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private async Task<object> CountAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: true);
        var count = await Apply(filter).CountAsync(ct);
        return new { filter = filter.Describe(), quantidade = count };
    }

    private async Task<object> SumAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: true);
        var query = Apply(filter);
        var count = await query.CountAsync(ct);
        var total = count == 0 ? 0m : await query.SumAsync(o => o.Valor.Amount, ct);
        return new
        {
            filter = filter.Describe(),
            quantidade = count,
            valorTotal = total,
            ticketMedio = count == 0 ? 0m : Math.Round(total / count, 2)
        };
    }

    private async Task<object> AverageProcessingTimeAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: false) with { Status = OrderStatus.Finalizado };

        // Do registro Pendente (criação) até o registro Finalizado no histórico.
        var spans = await Apply(filter)
            .Select(o => new
            {
                Inicio = o.Historico.Where(h => h.Status == OrderStatus.Pendente).Min(h => h.Data),
                Fim = o.Historico.Where(h => h.Status == OrderStatus.Finalizado).Max(h => h.Data)
            })
            .ToListAsync(ct);

        var seconds = spans.Select(s => (s.Fim - s.Inicio).TotalSeconds).ToList();
        return new
        {
            filter = filter.Describe(),
            medida = "Tempo entre a criação do pedido (Pendente) e a finalização (Finalizado).",
            pedidosConsiderados = seconds.Count,
            mediaSegundos = seconds.Count == 0 ? (double?)null : Math.Round(seconds.Average(), 2),
            minimoSegundos = seconds.Count == 0 ? (double?)null : Math.Round(seconds.Min(), 2),
            maximoSegundos = seconds.Count == 0 ? (double?)null : Math.Round(seconds.Max(), 2)
        };
    }

    private async Task<object> ByStatusAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken ct)
    {
        var filter = ReadFilter(input, allowStatus: false);
        var rows = await Apply(filter)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count(), ValorTotal = g.Sum(o => o.Valor.Amount) })
            .ToListAsync(ct);

        return new
        {
            filter = filter.Describe(),
            porStatus = Enum.GetValues<OrderStatus>().Select(status =>
            {
                var row = rows.SingleOrDefault(r => r.Status == status);
                return new { status = status.ToString(), quantidade = row?.Quantidade ?? 0, valorTotal = row?.ValorTotal ?? 0m };
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
                throw new ToolInputException($"'limit' deve ser um inteiro entre 1 e {MaxListLimit}.");
            }
        }

        var orders = await Apply(filter)
            .OrderByDescending(o => o.DataCriacao)
            .Take(limit)
            .Select(o => new { o.Id, o.Cliente, o.Produto, Valor = o.Valor.Amount, o.Status, o.DataCriacao })
            .ToListAsync(ct);

        return new
        {
            filter = filter.Describe(),
            pedidos = orders.Select(o => new
            {
                id = o.Id,
                cliente = o.Cliente,
                produto = o.Produto,
                valor = o.Valor,
                status = o.Status.ToString(),
                dataCriacao = TimeZoneInfo.ConvertTime(o.DataCriacao, timeZone).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
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
            query = query.Where(o => o.DataCriacao >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(o => o.DataCriacao < to);
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
                throw new ToolInputException("'status' deve ser Pendente, Processando ou Finalizado.");
            }

            status = parsed;
        }

        var from = ReadDate(input, "from");
        var to = ReadDate(input, "to");
        if (from is not null && to is not null && from >= to)
        {
            throw new ToolInputException("'from' deve ser anterior a 'to'.");
        }

        return new OrderFilter(status, from, to);
    }

    /// <summary>Aceita data (yyyy-MM-dd) ou data/hora ISO-8601; sem fuso, usa o fuso configurado.</summary>
    private DateTimeOffset? ReadDate(IReadOnlyDictionary<string, JsonElement> input, string key)
    {
        if (!input.TryGetValue(key, out var raw) || raw.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var text = raw.ValueKind == JsonValueKind.String ? raw.GetString() : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ToolInputException($"'{key}' deve ser uma data ISO-8601.");
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

        throw new ToolInputException($"'{key}' deve ser uma data ISO-8601 (ex.: 2026-11-01 ou 2026-11-01T08:00:00).");
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

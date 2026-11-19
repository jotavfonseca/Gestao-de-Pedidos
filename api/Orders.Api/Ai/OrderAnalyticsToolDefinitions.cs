using System.Text.Json;
using Anthropic.Models.Beta.Messages;

namespace Orders.Api.Ai;

/// <summary>Esquemas das ferramentas expostas ao Claude.</summary>
internal static class OrderAnalyticsToolDefinitions
{
    private static readonly JsonElement Status = Schema(new
    {
        type = "string",
        @enum = new[] { "Pendente", "Processando", "Finalizado" },
        description = "Filtra por status (opcional)."
    });

    private static readonly JsonElement From = Schema(new
    {
        type = "string",
        description = "Início do período, inclusivo, sobre a data de criação. ISO-8601: 2026-11-01 ou 2026-11-01T08:00:00 (sem fuso = America/Sao_Paulo)."
    });

    private static readonly JsonElement To = Schema(new
    {
        type = "string",
        description = "Fim do período, exclusivo, sobre a data de criação. Mesmo formato de 'from'."
    });

    public static readonly List<BetaToolUnion> All =
    [
        Tool(OrderAnalyticsTools.CountOrders,
            "Conta pedidos, com filtros opcionais de status e período de criação.",
            new() { ["status"] = Status, ["from"] = From, ["to"] = To }),

        Tool(OrderAnalyticsTools.SumOrderValues,
            "Soma o valor dos pedidos (em reais) e calcula o ticket médio, com filtros opcionais de status e período de criação.",
            new() { ["status"] = Status, ["from"] = From, ["to"] = To }),

        Tool(OrderAnalyticsTools.AverageProcessingTime,
            "Tempo médio, mínimo e máximo entre a criação (Pendente) e a finalização (Finalizado) dos pedidos finalizados, " +
            "opcionalmente só dos criados no período.",
            new() { ["from"] = From, ["to"] = To }),

        Tool(OrderAnalyticsTools.OrdersByStatus,
            "Quantidade e valor total de pedidos agrupados por status, opcionalmente só dos criados no período.",
            new() { ["from"] = From, ["to"] = To }),

        Tool(OrderAnalyticsTools.ListRecentOrders,
            "Lista os pedidos mais recentes (id, cliente, produto, valor, status, data de criação).",
            new()
            {
                ["limit"] = Schema(new { type = "integer", minimum = 1, maximum = 20, description = "Quantidade (padrão 10)." }),
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

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Orders.Api.Contracts;

public sealed class AskRequest
{
    /// <example>Quantos pedidos estão pendentes?</example>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Escreva uma pergunta.")]
    [StringLength(500, ErrorMessage = "A pergunta deve ter no máximo {1} caracteres.")]
    public string? Question { get; init; }
}

public sealed record AskResponse(string Answer, IReadOnlyList<ToolCallResponse> ToolCalls);

public sealed record ToolCallResponse(string Name, IReadOnlyDictionary<string, JsonElement> Input, bool IsError);

public sealed record AskStatusResponse(bool Enabled, string? Model);

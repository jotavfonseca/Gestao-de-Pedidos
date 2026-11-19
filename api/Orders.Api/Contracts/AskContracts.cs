using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Orders.Api.Contracts;

public sealed class AskRequest
{
    /// <example>How many orders are pending?</example>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Please type a question.")]
    [StringLength(500, ErrorMessage = "The question must be at most {1} characters.")]
    public string? Question { get; init; }
}

public sealed record AskResponse(string Answer, IReadOnlyList<ToolCallResponse> ToolCalls);

public sealed record ToolCallResponse(string Name, IReadOnlyDictionary<string, JsonElement> Input, bool IsError);

public sealed record AskStatusResponse(bool Enabled, string? Model);

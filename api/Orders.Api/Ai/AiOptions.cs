namespace Orders.Api.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Chave da API da Anthropic. Vazia = módulo desativado.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5-5";

    public int MaxTokens { get; set; } = 8000;

    /// <summary>Limite de idas e voltas de tool use por pergunta.</summary>
    public int MaxIterations { get; set; } = 6;

    /// <summary>Fuso usado para interpretar "hoje", "este mês" etc.</summary>
    public string TimeZone { get; set; } = "America/Sao_Paulo";

    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}

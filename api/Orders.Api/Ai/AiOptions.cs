namespace Orders.Api.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Anthropic API key. Empty = module disabled.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5-5";

    public int MaxTokens { get; set; } = 8000;

    /// <summary>Maximum tool-use round trips per question.</summary>
    public int MaxIterations { get; set; } = 6;

    /// <summary>Time zone used to interpret "today", "this month", etc.</summary>
    public string TimeZone { get; set; } = "America/Sao_Paulo";

    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}

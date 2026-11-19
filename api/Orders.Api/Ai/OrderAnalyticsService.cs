using System.Globalization;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Options;

namespace Orders.Api.Ai;

public sealed record AskResult(string Answer, IReadOnlyList<ToolCallInfo> ToolCalls);

public sealed record ToolCallInfo(string Name, IReadOnlyDictionary<string, JsonElement> Input, bool IsError);

/// <summary>O módulo de IA está desligado (sem ANTHROPIC_API_KEY).</summary>
public sealed class AiDisabledException() : Exception("Módulo de IA desativado: configure ANTHROPIC_API_KEY.");

/// <summary>Falha ao falar com a API da Anthropic (limite, indisponibilidade, erro).</summary>
public sealed class AiUnavailableException(string message, Exception inner, bool transient) : Exception(message, inner)
{
    public bool Transient { get; } = transient;
}

public interface IOrderAnalyticsService
{
    bool Enabled { get; }

    Task<AskResult> AskAsync(string question, CancellationToken cancellationToken);
}

internal sealed class DisabledOrderAnalyticsService : IOrderAnalyticsService
{
    public bool Enabled => false;

    public Task<AskResult> AskAsync(string question, CancellationToken cancellationToken) => throw new AiDisabledException();
}

/// <summary>
/// Responde perguntas em linguagem natural sobre os pedidos. O Claude
/// interpreta a pergunta, chama as ferramentas de <see cref="OrderAnalyticsTools"/>
/// (consultas fixas, somente leitura) e redige a resposta com os números reais.
/// </summary>
internal sealed class ClaudeOrderAnalyticsService(
    AnthropicClient client,
    OrderAnalyticsTools tools,
    TimeZoneInfo timeZone,
    TimeProvider clock,
    IOptions<AiOptions> options,
    ILogger<ClaudeOrderAnalyticsService> logger) : IOrderAnalyticsService
{
    private const string FallbackBeta = "server-side-fallback-2026-07-01";

    private const string SystemPrompt = """
        Você é o assistente de análise do sistema "Gestão de Pedidos".
        Responda em português do Brasil, de forma amigável e direta, usando apenas
        dados obtidos pelas ferramentas disponíveis. Nunca invente números.

        Regras:
        - Sempre consulte uma ferramenta antes de citar qualquer quantidade, valor ou tempo.
        - Converta períodos relativos ("hoje", "ontem", "esta semana", "este mês") em
          datas usando a data atual e o fuso informados na mensagem do usuário.
          Use 'from' inclusivo e 'to' exclusivo (ex.: hoje = de hoje 00:00 até amanhã 00:00).
        - Status possíveis: Pendente, Processando, Finalizado. "Aprovar" um pedido
          significa levá-lo até Finalizado.
        - Valores monetários em reais, no formato brasileiro (ex.: R$ 1.234,50).
          Tempos em unidades legíveis (segundos, minutos, horas).
        - Diga de forma breve o que foi medido e o período considerado.
        - Você só tem acesso de leitura. Se pedirem para criar, alterar ou apagar
          pedidos, explique educadamente que não pode fazer isso.
        - Se a pergunta não for sobre os pedidos, diga que só responde sobre pedidos.
        """;

    public bool Enabled => true;

    public async Task<AskResult> AskAsync(string question, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone);
        var toolCalls = new List<ToolCallInfo>();

        List<BetaMessageParam> messages =
        [
            new BetaUserMessageParam
            {
                Content = new List<BetaContentBlockParam>
                {
                    new BetaTextBlockParam
                    {
                        Text = $"Data e hora atual: {now.ToString("yyyy-MM-dd HH:mm (dddd)", CultureInfo.GetCultureInfo("pt-BR"))} " +
                               $"— fuso {settings.TimeZone} (UTC{now:zzz})."
                    },
                    new BetaTextBlockParam { Text = question }
                }
            }
        ];

        for (var iteration = 1; iteration <= settings.MaxIterations; iteration++)
        {
            var response = await SendAsync(settings, messages, cancellationToken);

            if (response.StopReason == "refusal")
            {
                logger.LogWarning("Pergunta recusada pelo modelo: {Details}", response.StopDetails);
                return new AskResult("Não posso responder a essa pergunta. Tente perguntar algo sobre os pedidos.", toolCalls);
            }

            var assistantContent = new List<BetaContentBlockParam>();
            var toolResults = new List<BetaContentBlockParam>();
            var answer = new List<string>();

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out var text))
                {
                    assistantContent.Add(new BetaTextBlockParam { Text = text.Text });
                    answer.Add(text.Text);
                }
                else if (block.TryPickThinking(out var thinking))
                {
                    // Blocos de thinking voltam inalterados (com assinatura) para o modelo.
                    assistantContent.Add(new BetaThinkingBlockParam { Thinking = thinking.Thinking, Signature = thinking.Signature });
                }
                else if (block.TryPickRedactedThinking(out var redacted))
                {
                    assistantContent.Add(new BetaRedactedThinkingBlockParam { Data = redacted.Data });
                }
                else if (block.TryPickToolUse(out var toolUse))
                {
                    assistantContent.Add(new BetaToolUseBlockParam { ID = toolUse.ID, Name = toolUse.Name, Input = toolUse.Input });
                    toolResults.Add(await RunToolAsync(toolUse.ID, toolUse.Name, toolUse.Input, toolCalls, cancellationToken));
                }
            }

            if (response.StopReason != "tool_use" || toolResults.Count == 0)
            {
                var text = string.Join("\n\n", answer).Trim();
                return new AskResult(text.Length > 0 ? text : "Não consegui formular uma resposta. Tente reformular a pergunta.", toolCalls);
            }

            messages.Add(new BetaAssistantMessageParam { Content = assistantContent });
            messages.Add(new BetaUserMessageParam { Content = toolResults });
        }

        logger.LogWarning("Limite de {MaxIterations} iterações de tool use atingido.", settings.MaxIterations);
        return new AskResult("A pergunta exigiu consultas demais. Tente algo mais específico.", toolCalls);
    }

    private async Task<BetaMessage> SendAsync(AiOptions settings, List<BetaMessageParam> messages, CancellationToken cancellationToken)
    {
        try
        {
            return await client.Beta.Messages.Create(new MessageCreateParams
            {
                Model = settings.Model,
                MaxTokens = settings.MaxTokens,
                System = SystemPrompt,
                Tools = OrderAnalyticsToolDefinitions.All,
                Messages = messages,
                // Se o modelo recusar por política, o servidor tenta outro modelo na mesma chamada.
                Betas = [FallbackBeta],
                Fallbacks = new Default()
            }, cancellationToken);
        }
        catch (AnthropicRateLimitException ex)
        {
            throw new AiUnavailableException("Limite de uso da IA atingido. Tente novamente em instantes.", ex, transient: true);
        }
        catch (Anthropic5xxException ex)
        {
            throw new AiUnavailableException("Serviço de IA indisponível no momento.", ex, transient: true);
        }
        catch (AnthropicApiException ex)
        {
            throw new AiUnavailableException("Falha ao consultar o serviço de IA.", ex, transient: false);
        }
        catch (HttpRequestException ex)
        {
            throw new AiUnavailableException("Não foi possível conectar ao serviço de IA.", ex, transient: true);
        }
    }

    private async Task<BetaContentBlockParam> RunToolAsync(
        string id,
        string name,
        IReadOnlyDictionary<string, JsonElement> input,
        List<ToolCallInfo> toolCalls,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await tools.ExecuteAsync(name, input, cancellationToken);
            toolCalls.Add(new ToolCallInfo(name, input, IsError: false));
            logger.LogInformation("Ferramenta {Tool} executada com {Input}.", name, JsonSerializer.Serialize(input));
            return new BetaToolResultBlockParam { ToolUseID = id, Content = result };
        }
        catch (ToolInputException ex)
        {
            toolCalls.Add(new ToolCallInfo(name, input, IsError: true));
            return new BetaToolResultBlockParam { ToolUseID = id, Content = ex.Message, IsError = true };
        }
    }
}

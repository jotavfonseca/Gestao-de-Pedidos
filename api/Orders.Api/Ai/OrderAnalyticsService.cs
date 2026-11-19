using System.Globalization;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Options;

namespace Orders.Api.Ai;

public sealed record AskResult(string Answer, IReadOnlyList<ToolCallInfo> ToolCalls);

public sealed record ToolCallInfo(string Name, IReadOnlyDictionary<string, JsonElement> Input, bool IsError);

/// <summary>The AI module is turned off (no ANTHROPIC_API_KEY).</summary>
public sealed class AiDisabledException() : Exception("AI module disabled: set ANTHROPIC_API_KEY.");

/// <summary>Failure talking to the Anthropic API (rate limit, outage, error).</summary>
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
/// Answers natural-language questions about orders. Claude interprets the
/// question, calls the <see cref="OrderAnalyticsTools"/> (fixed, read-only
/// queries) and writes the answer with the real numbers.
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
        You are the analytics assistant of the "Order Management" system.
        Answer in English, in a friendly and direct way, using only data returned
        by the available tools. Never make up numbers.

        Rules:
        - Always call a tool before stating any count, amount or duration.
        - Turn relative periods ("today", "yesterday", "this week", "this month") into
          dates using the current date and time zone given in the user's message.
          Use an inclusive 'from' and an exclusive 'to' (e.g. today = from today 00:00 to tomorrow 00:00).
        - Possible statuses: Pending, Processing, Completed. "Approving" an order
          means taking it to Completed.
        - Format amounts in US dollars (e.g. $1,234.50) and durations in readable
          units (seconds, minutes, hours).
        - Briefly say what was measured and for which period.
        - You only have read access. If asked to create, change or delete orders,
          politely explain that you cannot do that.
        - If the question is not about orders, say you can only answer questions about orders.
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
                        Text = $"Current date and time: {now.ToString("yyyy-MM-dd HH:mm (dddd)", CultureInfo.InvariantCulture)} " +
                               $"— time zone {settings.TimeZone} (UTC{now:zzz})."
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
                logger.LogWarning("Question refused by the model: {Details}", response.StopDetails);
                return new AskResult("I can't answer that question. Try asking something about the orders.", toolCalls);
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
                    // Thinking blocks go back to the model unchanged (with their signature).
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
                return new AskResult(text.Length > 0 ? text : "I couldn't put together an answer. Try rephrasing the question.", toolCalls);
            }

            messages.Add(new BetaAssistantMessageParam { Content = assistantContent });
            messages.Add(new BetaUserMessageParam { Content = toolResults });
        }

        logger.LogWarning("Reached the limit of {MaxIterations} tool-use iterations.", settings.MaxIterations);
        return new AskResult("This question needed too many queries. Try something more specific.", toolCalls);
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
                // If the model refuses for policy reasons, the server retries with another model in the same call.
                Betas = [FallbackBeta],
                Fallbacks = new Default()
            }, cancellationToken);
        }
        catch (AnthropicRateLimitException ex)
        {
            throw new AiUnavailableException("AI rate limit reached. Please try again shortly.", ex, transient: true);
        }
        catch (Anthropic5xxException ex)
        {
            throw new AiUnavailableException("The AI service is unavailable right now.", ex, transient: true);
        }
        catch (AnthropicApiException ex)
        {
            throw new AiUnavailableException("The AI service request failed.", ex, transient: false);
        }
        catch (HttpRequestException ex)
        {
            throw new AiUnavailableException("Could not connect to the AI service.", ex, transient: true);
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
            logger.LogInformation("Tool {Tool} executed with {Input}.", name, JsonSerializer.Serialize(input));
            return new BetaToolResultBlockParam { ToolUseID = id, Content = result };
        }
        catch (ToolInputException ex)
        {
            toolCalls.Add(new ToolCallInfo(name, input, IsError: true));
            return new BetaToolResultBlockParam { ToolUseID = id, Content = ex.Message, IsError = true };
        }
    }
}

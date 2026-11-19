using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orders.Api.Ai;
using Orders.Api.Contracts;

namespace Orders.Api.Controllers;

/// <summary>"Ask about your orders" module (AI).</summary>
[ApiController]
[Route("orders/ask")]
public sealed class AskController(IOrderAnalyticsService analytics, IOptions<AiOptions> options) : ControllerBase
{
    /// <summary>Tells whether the AI module is enabled.</summary>
    [HttpGet("status")]
    public AskStatusResponse Status() =>
        new(analytics.Enabled, analytics.Enabled ? options.Value.Model : null);

    /// <summary>Answers a natural-language question using real order data.</summary>
    [HttpPost]
    [ProducesResponseType<AskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AskResponse>> Ask(AskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await analytics.AskAsync(request.Question!.Trim(), cancellationToken);
            return new AskResponse(
                result.Answer,
                result.ToolCalls.Select(c => new ToolCallResponse(c.Name, c.Input, c.IsError)).ToList());
        }
        catch (AiDisabledException ex)
        {
            return Problem(title: "AI module disabled.", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (AiUnavailableException ex)
        {
            return Problem(
                title: "AI service unavailable.",
                detail: ex.Message,
                statusCode: ex.Transient ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status502BadGateway);
        }
    }
}

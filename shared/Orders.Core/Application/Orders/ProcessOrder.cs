using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Core.Domain.Orders;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Core.Application.Orders;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";

    /// <summary>Tempo entre Processando e Finalizado (desafio: 5 s).</summary>
    public int DelaySeconds { get; set; } = 5;
}

public enum ProcessOrderOutcome
{
    /// <summary>Esta execução levou o pedido até Finalizado.</summary>
    Finalized,

    /// <summary>O pedido já estava finalizado: mensagem duplicada, nada a fazer.</summary>
    AlreadyFinalized,

    /// <summary>O pedido não existe.</summary>
    OrderNotFound
}

/// <summary>
/// Processa um pedido de forma idempotente. O estado atual do agregado decide
/// o que fazer, e a concorrência otimista (xmin) impede que duas entregas da
/// mesma mensagem façam a mesma transição:
/// <list type="bullet">
/// <item>Pendente → Processando → (espera) → Finalizado</item>
/// <item>Processando (reentrega após falha) → Finalizado</item>
/// <item>Finalizado → nada</item>
/// </list>
/// </summary>
public sealed class ProcessOrderHandler(
    OrderContext db,
    TimeProvider clock,
    IOptions<ProcessingOptions> options,
    ILogger<ProcessOrderHandler> logger)
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<ProcessOrderOutcome> HandleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ProcessAsync(orderId, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                // Outra entrega da mesma mensagem mudou o pedido antes: recarrega e reavalia.
                logger.LogInformation(
                    "Conflito de concorrência no pedido {OrderId} (tentativa {Attempt}); reavaliando o estado.",
                    orderId, attempt);
                db.ChangeTracker.Clear();
            }
        }
    }

    private async Task<ProcessOrderOutcome> ProcessAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Historico)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return ProcessOrderOutcome.OrderNotFound;
        }

        if (order.Status == OrderStatus.Finalizado)
        {
            logger.LogInformation("Pedido {OrderId} já finalizado; mensagem ignorada (idempotência).", orderId);
            return ProcessOrderOutcome.AlreadyFinalized;
        }

        if (order.Status == OrderStatus.Pendente)
        {
            order.TransitionTo(OrderStatus.Processando, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Pedido {OrderId} em processamento.", orderId);
        }
        else
        {
            logger.LogInformation("Pedido {OrderId} já estava em processamento; retomando.", orderId);
        }

        var delay = TimeSpan.FromSeconds(Math.Max(0, options.Value.DelaySeconds));
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, clock, cancellationToken);
        }

        order.TransitionTo(OrderStatus.Finalizado, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Pedido {OrderId} finalizado.", orderId);

        return ProcessOrderOutcome.Finalized;
    }
}

using Microsoft.Extensions.Options;

namespace Orders.Worker;

/// <summary>
/// O worker não expõe HTTP; o healthcheck do Compose verifica se este
/// arquivo foi atualizado no último minuto.
/// </summary>
internal sealed class Heartbeat(IOptions<WorkerOptions> options, TimeProvider clock, ILogger<Heartbeat> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.HeartbeatIntervalSeconds), clock);

        try
        {
            do
            {
                try
                {
                    await File.WriteAllTextAsync(settings.HeartbeatFile, clock.GetUtcNow().ToString("O"), stoppingToken);
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Não foi possível atualizar o heartbeat em {File}.", settings.HeartbeatFile);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal.
        }
    }
}

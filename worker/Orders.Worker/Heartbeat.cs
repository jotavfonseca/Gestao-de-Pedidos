using Microsoft.Extensions.Options;

namespace Orders.Worker;

/// <summary>
/// The worker has no HTTP endpoint; the Compose healthcheck checks that this
/// file was updated within the last minute.
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
                    logger.LogWarning(ex, "Could not update the heartbeat at {File}.", settings.HeartbeatFile);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }
}

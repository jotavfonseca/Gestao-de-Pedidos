namespace Orders.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>Mensagens processadas em paralelo.</summary>
    public int MaxConcurrentCalls { get; set; } = 4;

    /// <summary>Arquivo tocado periodicamente; usado pelo healthcheck do Docker Compose.</summary>
    public string HeartbeatFile { get; set; } = Path.Combine(Path.GetTempPath(), "orders-worker.heartbeat");

    public int HeartbeatIntervalSeconds { get; set; } = 10;
}

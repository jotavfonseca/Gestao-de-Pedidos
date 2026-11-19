namespace Orders.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>Messages processed in parallel.</summary>
    public int MaxConcurrentCalls { get; set; } = 4;

    /// <summary>File touched periodically; used by the Docker Compose healthcheck.</summary>
    public string HeartbeatFile { get; set; } = Path.Combine(Path.GetTempPath(), "orders-worker.heartbeat");

    public int HeartbeatIntervalSeconds { get; set; } = 10;
}

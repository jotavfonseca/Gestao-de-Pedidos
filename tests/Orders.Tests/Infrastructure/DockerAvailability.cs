using DotNet.Testcontainers.Configurations;

namespace Orders.Tests.Infrastructure;

internal static class DockerAvailability
{
    private static readonly Lazy<bool> Available = new(() =>
    {
        try
        {
            return TestcontainersSettings.OS.DockerEndpointAuthConfig is not null;
        }
        catch
        {
            return false;
        }
    });

    public static bool IsAvailable => Available.Value;
}

/// <summary>A test that needs Docker; skipped with a notice when Docker is not available.</summary>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker is unavailable: integration test (Testcontainers) skipped.";
        }
    }
}

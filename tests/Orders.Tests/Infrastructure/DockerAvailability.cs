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

/// <summary>Teste que precisa de Docker; é pulado com aviso quando o Docker não está disponível.</summary>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker indisponível: teste de integração (Testcontainers) pulado.";
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Orders.Core.Observability;

public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Liga o tracing (OpenTelemetry → OTLP) somente se OTEL_EXPORTER_OTLP_ENDPOINT
    /// estiver configurado; sem ele, a aplicação roda normalmente sem exportar.
    /// </summary>
    public static IServiceCollection AddOrdersTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing = null)
    {
        if (string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(Telemetry.MessagingSourceName)
                    .AddNpgsql()
                    .AddOtlpExporter();

                configureTracing?.Invoke(tracing);
            });

        return services;
    }
}

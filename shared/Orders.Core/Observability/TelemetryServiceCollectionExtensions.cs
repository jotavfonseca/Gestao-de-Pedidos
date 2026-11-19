using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Orders.Core.Observability;

public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Enables tracing (OpenTelemetry → OTLP) only when OTEL_EXPORTER_OTLP_ENDPOINT is
    /// set; without it the application runs normally and exports nothing.
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

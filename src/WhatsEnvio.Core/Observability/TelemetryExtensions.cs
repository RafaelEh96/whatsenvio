using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WhatsEnvio.Core.Observability;

public static class TelemetryExtensions
{
    public static IServiceCollection AddWhatsEnvioTelemetry(
        this IServiceCollection services,
        string serviceName,
        bool useConsoleExporter = false)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource(WhatsEnvioTelemetry.ActivitySourceName);
                if (useConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(WhatsEnvioTelemetry.MeterName);
                metrics.AddRuntimeInstrumentation();
            });

        return services;
    }
}

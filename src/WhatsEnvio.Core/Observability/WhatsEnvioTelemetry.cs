using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WhatsEnvio.Core.Observability;

public static class WhatsEnvioTelemetry
{
    public const string ActivitySourceName = "WhatsEnvio";
    public const string MeterName = "WhatsEnvio";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);
}

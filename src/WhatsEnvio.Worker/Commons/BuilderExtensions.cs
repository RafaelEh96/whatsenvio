using WhatsEnvio.Core.Observability;

namespace WhatsEnvio.Worker.Commons;

public static class BuilderExtensions
{
    extension(HostApplicationBuilder builder)
    {
        public void AddTelemetry()
        {
            builder.Services.AddWhatsEnvioTelemetry(
                serviceName: "whatsenvio-worker",
                useConsoleExporter: builder.Environment.IsDevelopment());
        }
    }
}

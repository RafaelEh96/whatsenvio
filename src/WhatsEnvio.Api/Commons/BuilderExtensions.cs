using OpenTelemetry.Trace;
using WhatsEnvio.Core.Observability;
using WhatsEnvio.Modules.Tenancy.Infrastructure;

namespace WhatsEnvio.Api.Commons;

public static class BuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public void AddConfigurations()
        {
            Configurations.ConnectionString = builder.Configuration.GetConnectionString("WhatsEnvio") ?? string.Empty;
        }

        public void AddDbContext()
        {
            builder.Services.AddTenancyModule(Configurations.ConnectionString);
        }

        public void AddDocumentation()
        {
            builder.Services.AddOpenApi();
        }

        public void AddTelemetry()
        {
            builder.Services.AddWhatsEnvioTelemetry(
                serviceName: "whatsenvio-api",
                useConsoleExporter: builder.Environment.IsDevelopment());

            builder.Services.AddOpenTelemetry()
                .WithTracing(t => t.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());
        }

    }
}

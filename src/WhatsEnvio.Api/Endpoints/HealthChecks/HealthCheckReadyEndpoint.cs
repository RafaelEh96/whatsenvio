using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using WhatsEnvio.Api.Commons;

namespace WhatsEnvio.Api.Endpoints.HealthChecks;

public class HealthCheckReadyEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        })
        .WithName("HealthCheckReady");
}

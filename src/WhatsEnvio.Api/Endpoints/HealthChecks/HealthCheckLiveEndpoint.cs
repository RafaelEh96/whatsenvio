using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using WhatsEnvio.Api.Commons;

namespace WhatsEnvio.Api.Endpoints.HealthChecks;

public class HealthCheckLiveEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapHealthChecks("/live", new HealthCheckOptions
        {
            Predicate = _ => false
        })
            .WithName("HealthCheckLive");
}

using WhatsEnvio.Api.Commons;
using WhatsEnvio.Api.Endpoints.HealthChecks;

namespace WhatsEnvio.Api.Endpoints;

public static class Endpoint
{
    public static void MapEndpoints(this WebApplication app)
    {
        app.MapGroup("/health")
            .WithTags("Health Checks")
            .MapEndpoint<HealthCheckLiveEndpoint>()
            .MapEndpoint<HealthCheckReadyEndpoint>();
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app) where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}

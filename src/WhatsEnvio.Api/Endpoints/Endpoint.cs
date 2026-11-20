using WhatsEnvio.Api.Authorization;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Api.Endpoints.HealthChecks;
using WhatsEnvio.Api.Endpoints.Identity;
using WhatsEnvio.Core.Tenancy;

namespace WhatsEnvio.Api.Endpoints;

public static class Endpoint
{
    public static void MapEndpoints(this WebApplication app)
    {
        const string apiVersion = "v1";
        const string baseUrl = $"/api/{apiVersion}";

        var tenants = app.MapGroup($"{baseUrl}/tenants/{{tenantId:guid}}")
            .RequireAuthorization(TenantPolicies.Member);

        tenants.MapGet("/member", (ITenantContext context) => TypedResults.Ok(new { context.TenantId}))
            .RequireAuthorization(TenantPolicies.Member);

        tenants.MapGet("/admin", (ITenantContext context) => TypedResults.Ok(new { context.TenantId }))
            .RequireAuthorization(TenantPolicies.Admin);

        tenants.MapGet("/owner", (ITenantContext context) => TypedResults.Ok(new { context.TenantId }))
            .RequireAuthorization(TenantPolicies.Owner);

        app.MapGroup("/health")
            .WithTags("Health Checks")
            .MapEndpoint<HealthCheckLiveEndpoint>()
            .MapEndpoint<HealthCheckReadyEndpoint>();

        app.MapGroup($"{baseUrl}/auth")
            .WithTags("Auth")
            .AddEndpointFilter<AntiForgeryValidationFilter>()
            .MapEndpoint<LoginEndpoint>()
            .MapEndpoint<AntiForgeryEndpoint>()
            .MapEndpoint<UserDetailsEndpoint>()
            .MapEndpoint<LogoutEndpoint>()
            .MapEndpoint<RevokeSessionsEndpoint>();

        app.MapGroup($"{baseUrl}/users")
            .WithTags("Users")
            .MapEndpoint<ForgotPasswordEndpoint>()
            .MapEndpoint<ResetPasswordEndpoint>();
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app) where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}

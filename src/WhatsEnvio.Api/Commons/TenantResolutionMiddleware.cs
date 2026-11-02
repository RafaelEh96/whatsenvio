using System.Security.Claims;
using WhatsEnvio.Core.Tenancy;
using WhatsEnvio.Modules.Tenancy.Contracts;

namespace WhatsEnvio.Api.Commons;

/// <summary>Marks endpoints whose tenantId route value must be authorized.</summary>
public sealed class RequireTenantMetadata;

public static class TenantEndpointExtensions
{
    public static TBuilder RequireTenant<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpoint => endpoint.Metadata.Add(new RequireTenantMetadata()));
        builder.RequireAuthorization();
        return builder;
    }
}

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IMembershipService memberships, ITenantContextAccessor tenantContext)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<RequireTenantMetadata>() is null)
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!Guid.TryParse(context.Request.RouteValues["tenantId"]?.ToString(), out var tenantId) || tenantId == Guid.Empty)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var membership = await memberships.GetActiveAsync(userId, tenantId, context.RequestAborted);
        if (membership is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        using (tenantContext.BeginScope(tenantId))
            await next(context);
    }
}

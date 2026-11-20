using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using System.Diagnostics;
using System.Security.Claims;
using WhatsEnvio.Core.Tenancy;

namespace WhatsEnvio.Api.Authorization;

public sealed class TenantAuthorizationMiddlewareResultHandler(
    TenantAuthorizationState state,
    ITenantContextAccessor tenantContext,
    ILogger<TenantAuthorizationMiddlewareResultHandler> logger) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var isTenatPolicy = policy.Requirements.OfType<TenantRequirement>().Any();

        if (!isTenatPolicy || !authorizeResult.Succeeded)
        {
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdValue = context.Request.RouteValues["tenantId"]?.ToString();

        if (!Guid.TryParse(userIdValue, out var userId)
             || userId == Guid.Empty
             || !Guid.TryParse(tenantIdValue, out var tenantId)
             || tenantId == Guid.Empty)
        {
            await ForbidAsync(next, context, policy);
            return;
        }

        if (!state.TryGetMembership(userId, tenantId, out var membership)
            || membership is null
            || membership.UserId != userId
            || membership.TenantId != tenantId)
        {
            await ForbidAsync(next, context, policy);
            return;
        }

        using (tenantContext.BeginScope(tenantId))
        using (logger.BeginScope(new Dictionary<string, object> { ["TenantId"] = tenantId }))
        {
            Activity.Current?.SetTag("tenant.id", tenantId.ToString());

            logger.LogInformation("Autorização de tenant concluída para {TenantId}", tenantId);

            await next(context);
        }
    }

    private Task ForbidAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy)
    {
        return _defaultHandler.HandleAsync(next, context, policy, PolicyAuthorizationResult.Forbid());
    }
}

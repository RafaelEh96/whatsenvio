using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WhatsEnvio.Modules.Tenancy.Contracts;

namespace WhatsEnvio.Api.Authorization;

public sealed class TenantAuthorizationHandler(
    IMembershipService membershipService,
    TenantAuthorizationState state)
    : AuthorizationHandler<TenantRequirement>
{

    private static readonly object MembershipKey = new();

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (context.Resource is not HttpContext httpContext)
            return;

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdValue = httpContext.Request.RouteValues["tenantId"]?.ToString();

        if (!Guid.TryParse(userIdValue, out var userId)
           || userId == Guid.Empty
           || !Guid.TryParse(tenantIdValue, out var tenantId)
           || tenantId == Guid.Empty)
            return;

        if (!state.TryGetMembership(userId, tenantId, out var membership))
        {
            membership = await membershipService.GetActiveAsync(
            userId,
            tenantId,
            httpContext.RequestAborted);

            state.StoreMembership(userId, tenantId, membership);
        }

        if (membership == null)
            return;

        if (membership.UserId != userId || membership.TenantId != tenantId)
            return;

        if (!requirement.AllowedRoles.Contains(membership.Role))
            return;

        context.Succeed(requirement);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WhatsEnvio.Core.Dtos.Membership;
using WhatsEnvio.Modules.Tenancy.Contracts;

namespace WhatsEnvio.Api.Authorization;

public sealed class TenantAuthorizationHandler(
    IMembershipService membershipService)
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

        if(!Guid.TryParse(userIdValue, out var userId)
           || !Guid.TryParse(tenantIdValue, out var tenantId))
            return;

        var membership = await GetMembership(
            httpContext,
            userId,
            tenantId);

        if (membership == null)
            return;

        if (!requirement.AllowedRoles.Contains(membership.Role))
            return;

        context.Succeed(requirement);
    }

    private async Task<MembershipResult?> GetMembership(
        HttpContext httpContext,
        Guid userId,
        Guid tenantId)
    {
        if (httpContext.Items.TryGetValue(MembershipKey, out var cached))
            return (MembershipResult?)cached;

        var membership = await membershipService.GetActiveAsync(
            userId,
            tenantId,
            httpContext.RequestAborted);

        httpContext.Items[MembershipKey] = membership;

        return membership;
    }

}

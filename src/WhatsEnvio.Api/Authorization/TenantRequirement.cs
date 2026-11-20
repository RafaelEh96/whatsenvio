using Microsoft.AspNetCore.Authorization;
using WhatsEnvio.Core.Enums;

namespace WhatsEnvio.Api.Authorization;

public class TenantRequirement(params MembershipRole[] allowedRoles)
    : IAuthorizationRequirement
{
    public IReadOnlySet<MembershipRole> AllowedRoles { get; }
        = new HashSet<MembershipRole>(allowedRoles);
}

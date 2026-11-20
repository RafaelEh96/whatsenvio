using WhatsEnvio.Core.Dtos.Membership;

namespace WhatsEnvio.Api.Authorization;

public sealed class TenantAuthorizationState
{
    private readonly Dictionary<(Guid UserId, Guid TenantId), MembershipResult?> _memberships = new();

    public bool TryGetMembership(Guid userId, Guid tenantId, out MembershipResult? membership)
    {
        return _memberships.TryGetValue((userId, tenantId), out membership);
    }

    public void StoreMembership(Guid userId, Guid tenantId, MembershipResult? membership)
    {
        _memberships[(userId, tenantId)] = membership;
    }
}

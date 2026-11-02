using WhatsEnvio.Core.Enums;
using WhatsEnvio.Modules.Tenancy.Tenants.Model;

namespace WhatsEnvio.Modules.Tenancy.Memberships.Model;

public class Membership
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsActive { get; set; }
    public MembershipRole Role { get; private set; }
    public Tenant Tenant { get; private set; } = null!;

    public Membership()
    {

    }

    public Membership(Guid tenantId, Guid userId, bool isActive, MembershipRole role)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        UserId = userId;
        Role = role;
        IsActive = isActive;
    }
}

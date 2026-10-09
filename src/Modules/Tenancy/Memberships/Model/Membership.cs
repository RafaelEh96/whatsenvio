namespace WhatsEnvio.Modules.Tenancy.Memberships.Model;

public class Membership
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public MembershipRole Role { get; private set; }

    public Membership()
    {
        
    }

    public Membership(Guid tenantId, Guid userId, MembershipRole role)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }
}

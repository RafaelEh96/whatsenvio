namespace WhatsEnvio.Core.Tenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
}

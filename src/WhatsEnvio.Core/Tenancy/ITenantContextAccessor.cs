namespace WhatsEnvio.Core.Tenancy;

public interface ITenantContextAccessor : ITenantContext
{
    IDisposable BeginScope(Guid tenantId);
}

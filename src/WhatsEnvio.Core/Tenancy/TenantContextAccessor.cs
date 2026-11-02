namespace WhatsEnvio.Core.Tenancy;

public class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<Guid?> CurrentTenant = new();

    public Guid? TenantId => CurrentTenant.Value;
    public IDisposable BeginScope(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("O tenant deve ser informado", nameof(tenantId));

        var previousTenantId = CurrentTenant.Value;
        CurrentTenant.Value = tenantId;

        return new Scope(previousTenantId);
    }

    private sealed class Scope(Guid? previousTenantId) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            CurrentTenant.Value = previousTenantId;
            _disposed = true;
        }
    }
}

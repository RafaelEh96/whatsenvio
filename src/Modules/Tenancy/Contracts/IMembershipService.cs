using WhatsEnvio.Core.Dtos.Membership;
using WhatsEnvio.Core.Dtos.Tenancy;

namespace WhatsEnvio.Modules.Tenancy.Contracts;

public interface IMembershipService
{
    Task<MembershipResult?> GetActiveAsync(Guid userId, Guid tenantId, CancellationToken token = default);
    Task<IReadOnlyList<TenantSummary>>  ListActiveAsync(Guid userId, CancellationToken token = default);
}

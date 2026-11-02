using WhatsEnvio.Core.Dtos.Membership;
using WhatsEnvio.Core.Dtos.Tenancy;

namespace WhatsEnvio.Modules.Tenancy.Contracts;

public interface IMembershipRepository
{
    Task<MembershipResult?> GetActiveAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken token = default);

    Task<IReadOnlyList<TenantSummary>> ListActiveByUserAsync(
        Guid userId,
        CancellationToken token = default);
}

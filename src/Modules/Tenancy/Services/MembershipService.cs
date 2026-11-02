using WhatsEnvio.Core.Dtos.Membership;
using WhatsEnvio.Core.Dtos.Tenancy;
using WhatsEnvio.Modules.Tenancy.Contracts;

namespace WhatsEnvio.Modules.Tenancy.Services;

public class MembershipService(IMembershipRepository repository) : IMembershipService
{
    public async Task<MembershipResult?> GetActiveAsync(Guid userId, Guid tenantId, CancellationToken token = default)
        => await repository.GetActiveAsync(userId, tenantId, token);

    public async Task<IReadOnlyList<TenantSummary>> ListActiveAsync(Guid userId, CancellationToken token = default)
        => await repository.ListActiveByUserAsync(userId, token);
}

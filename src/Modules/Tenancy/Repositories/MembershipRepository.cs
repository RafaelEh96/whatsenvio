using Microsoft.EntityFrameworkCore;
using WhatsEnvio.Core.Dtos.Membership;
using WhatsEnvio.Core.Dtos.Tenancy;
using WhatsEnvio.Modules.Tenancy.Contracts;
using WhatsEnvio.Modules.Tenancy.Persistence.Context;

namespace WhatsEnvio.Modules.Tenancy.Repositories;

public class MembershipRepository(TenancyDbContext context) : IMembershipRepository
{
    public async Task<MembershipResult?> GetActiveAsync(Guid userId, Guid tenantId, CancellationToken token = default)
    {
        var membership = await context
            .Memberships
            .AsNoTracking()
            .Where(x
                => x.UserId == userId
                   && x.TenantId == tenantId
                   && x.IsActive
                   && x.Tenant.IsActive)
            .FirstOrDefaultAsync(cancellationToken: token);

        if (membership == null)
            return null;

        var result = new MembershipResult(
            TenantId: membership.TenantId,
            UserId: membership.UserId,
            Role: membership.Role);

        return result;
    }

    public async Task<IReadOnlyList<TenantSummary>> ListActiveByUserAsync(Guid userId, CancellationToken token = default)
    {
        return await context
            .Memberships
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.IsActive
                        && x.Tenant.IsActive)
            .Select(x => new TenantSummary(
                x.Tenant.Id,
                x.Tenant.Name,
                x.Role))
            .ToListAsync(cancellationToken: token);
    }
}

using Microsoft.EntityFrameworkCore;
using WhatsEnvio.Core.Enums;
using WhatsEnvio.Modules.Tenancy.Memberships.Model;
using WhatsEnvio.Modules.Tenancy.Persistence.Context;
using WhatsEnvio.Modules.Tenancy.Repositories;
using WhatsEnvio.Modules.Tenancy.Tenants.Model;

namespace WhatsEnvio.IntegrationTests;

public class MembershipRepositoryTests
{
    [Fact]
    public async Task Consultas_retornam_apenas_vinculos_e_empresas_ativos()
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new TenancyDbContext(options);
        var user = Guid.NewGuid();
        var active = new Tenant("Ativo", "UTC", DateTimeOffset.UtcNow);
        var inactiveTenant = new Tenant("Inativo", "UTC", DateTimeOffset.UtcNow);
        inactiveTenant.Deactivate();
        var inactiveMembership = new Tenant("Vínculo inativo", "UTC", DateTimeOffset.UtcNow);
        db.Tenants.AddRange(active, inactiveTenant, inactiveMembership);
        db.Memberships.AddRange(
            new Membership(active.Id, user, true, MembershipRole.Admin),
            new Membership(inactiveTenant.Id, user, true, MembershipRole.Owner),
            new Membership(inactiveMembership.Id, user, false, MembershipRole.Owner));
        await db.SaveChangesAsync();
        var repository = new MembershipRepository(db);

        var summaries = await repository.ListActiveByUserAsync(user);
        var summary = Assert.Single(summaries);
        Assert.Equal(active.Id, summary.Id);
        Assert.Equal("Ativo", summary.Name);
        Assert.Equal(MembershipRole.Admin, summary.Role);
        Assert.NotNull(await repository.GetActiveAsync(user, active.Id));
        Assert.Null(await repository.GetActiveAsync(user, inactiveTenant.Id));
        Assert.Null(await repository.GetActiveAsync(user, inactiveMembership.Id));
        Assert.Null(await repository.GetActiveAsync(Guid.NewGuid(), active.Id));
    }
}

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WhatsEnvio.Modules.Tenancy.Audit;
using WhatsEnvio.Modules.Tenancy.Memberships.Model;
using WhatsEnvio.Modules.Tenancy.Tenants.Model;

namespace WhatsEnvio.Modules.Tenancy.Persistence.Context;

public class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options)
{

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<TenancyAuditEvent> AuditEvents => Set<TenancyAuditEvent>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("iam");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

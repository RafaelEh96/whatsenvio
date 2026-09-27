using Microsoft.EntityFrameworkCore;
using System.Reflection;
using WhatsEnvio.Modules.Tenancy.Domain;

namespace WhatsEnvio.Modules.Tenancy.Infrastructure.Context;

public class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options)
{

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Membership> Memberships => Set<Membership>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("iam");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatsEnvio.Modules.Identity.Audit;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Modules.Identity.Persistence.Context;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityUserContext<AppUser, Guid>(options)
{
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<IdentityAuditEvent> AuditEvents => Set<IdentityAuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("iam");
        builder.Entity<AppUser>().ToTable("asp_net_users");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("asp_net_user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("asp_net_user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("asp_net_user_tokens");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

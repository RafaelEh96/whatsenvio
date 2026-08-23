using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatsEnvio.Modules.Identity.Infrastructure.Models;

namespace WhatsEnvio.Modules.Identity.Infrastructure.Context;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityUserContext<AppUser, Guid>(options)
{
    public DbSet<AppUser> AppUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("iam");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

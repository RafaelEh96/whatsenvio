using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WhatsEnvio.Modules.Identity.Persistence;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("WHATSENVIO_DB") ??
                               "Host=127.0.0.1;Port=5432;Database=whatsenvio;Username=whatsenvio;Password=whatsenvio_local_only";
        var options = new DbContextOptionsBuilder<IdentityDbContext>();

        IdentityModuleExtension.ConfigurateIdentity(options, connectionString);

        return new IdentityDbContext(options.Options);
    }
}

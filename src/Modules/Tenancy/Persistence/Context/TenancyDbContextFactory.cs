using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WhatsEnvio.Modules.Tenancy.Persistence.Context;

namespace WhatsEnvio.Modules.Tenancy.Persistence;

public class TenancyDbContextFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    public TenancyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("WHATSENVIO_DB") ?? "Host=127.0.0.1;Port=5432;Database=whatsenvio;Username=whatsenvio;Password=whatsenvio_local_only";

        var options = new DbContextOptionsBuilder<TenancyDbContext>();

        TenancyModuleExtension.ConfigureTenancy(options, connectionString);

        return new TenancyDbContext(options.Options);
    }
}

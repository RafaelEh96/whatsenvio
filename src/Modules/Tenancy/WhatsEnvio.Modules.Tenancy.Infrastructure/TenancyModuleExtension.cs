using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WhatsEnvio.Modules.Tenancy.Infrastructure.Context;

namespace WhatsEnvio.Modules.Tenancy.Infrastructure;

public static class TenancyModuleExtension
{
    public static IServiceCollection AddTenancyModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<TenancyDbContext>(options =>
            ConfigureTenancy(options, connectionString));
        return services;
    }

    internal static DbContextOptionsBuilder ConfigureTenancy(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(connectionString, sql
            => sql.MigrationsHistoryTable("__ef_migrations_history", "iam"))
            .UseSnakeCaseNamingConvention();
        return options;
    }
}

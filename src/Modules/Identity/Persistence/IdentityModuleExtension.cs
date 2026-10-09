using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WhatsEnvio.Modules.Identity.Persistence;

namespace WhatsEnvio.Modules.Identity;

public static class IdentityModuleExtension
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            ConfigurateIdentity(options, connectionString)
        );

        services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>("identity-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddNpgsql());

        return services;
    }

    internal static DbContextOptionsBuilder ConfigurateIdentity(
        DbContextOptionsBuilder options,
        string connectionString)
    {
        options.UseNpgsql(connectionString, sql =>
            sql.MigrationsHistoryTable("__ef_migrations_history_identity", "iam"))
            .UseSnakeCaseNamingConvention();
        return options;
    }
}

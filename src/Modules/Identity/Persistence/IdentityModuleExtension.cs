using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WhatsEnvio.Modules.Identity.Audit;
using WhatsEnvio.Modules.Identity.Persistence.Context;

namespace WhatsEnvio.Modules.Identity.Persistence;

public static class IdentityModuleExtension
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            ConfigurateIdentity(options, connectionString)
        );

        services.AddScoped<IdentityAuditService>();

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

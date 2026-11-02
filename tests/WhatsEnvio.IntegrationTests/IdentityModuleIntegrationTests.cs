using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Modules.Identity;
using WhatsEnvio.Modules.Identity.Persistence;
using WhatsEnvio.Modules.Identity.Persistence.Context;
using WhatsEnvio.Modules.Tenancy;
using WhatsEnvio.Modules.Tenancy.Persistence;
using WhatsEnvio.Modules.Tenancy.Persistence.Context;

namespace WhatsEnvio.IntegrationTests;

public class IdentityModuleIntegrationTests
{
    [Fact]
    public void Cada_migracao_de_auditoria_cria_schema_de_forma_idempotente()
    {
        var services = new ServiceCollection();
        const string connection = "Host=localhost;Database=unused;Username=test;Password=test";
        services.AddIdentityModule(connection);
        services.AddTenancyModule(connection);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenancy = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        var identityMigrations = identity.Database.GetMigrations().ToArray();
        var tenancyMigrations = tenancy.Database.GetMigrations().ToArray();
        var identitySql = identity.GetService<IMigrator>().GenerateScript(identityMigrations[^2], identityMigrations[^1]);
        var tenancyAuditIndex = Array.FindIndex(tenancyMigrations, name => name.EndsWith("_TenancyAudit", StringComparison.Ordinal));
        Assert.True(tenancyAuditIndex > 0);
        var tenancySql = tenancy.GetService<IMigrator>().GenerateScript(tenancyMigrations[tenancyAuditIndex - 1], tenancyMigrations[tenancyAuditIndex]);
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS audit", identitySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS audit", tenancySql, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Migracoes_de_auditoria_funcionam_em_ambas_as_ordens(bool identityPrimeiro)
    {
        await using var container = new PostgreSqlBuilder("postgres:18.4-alpine3.23").Build();
        await container.StartAsync(TestContext.Current.CancellationToken);
        var services = new ServiceCollection();
        services.AddIdentityModule(container.GetConnectionString());
        services.AddTenancyModule(container.GetConnectionString());
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenancy = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        if (identityPrimeiro)
        {
            await identity.Database.MigrateAsync(TestContext.Current.CancellationToken);
            await tenancy.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }
        else
        {
            await tenancy.Database.MigrateAsync(TestContext.Current.CancellationToken);
            await identity.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        var tables = await identity.Database.SqlQueryRaw<string>(
            "SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'audit'")
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, tables.Count);
        Assert.Contains("identity_events", tables);
        Assert.Single(tables, table => table != "identity_events");

        foreach (var table in tables)
        {
            var columns = await identity.Database.SqlQueryRaw<string>(
                "SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_schema = 'audit' AND table_name = {0}", table)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(new[] { "id", "occurred_at_utc", "tenant_id", "user_id", "event_type", "data", "correlation_id" }.Order(), columns.Order());
            var jsonType = await identity.Database.SqlQueryRaw<string>(
                "SELECT data_type AS \"Value\" FROM information_schema.columns WHERE table_schema = 'audit' AND table_name = {0} AND column_name = 'data'", table)
                .SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal("jsonb", jsonType);
        }
    }

    [Fact]
    public async Task Migracoes_de_identity_e_tenancy_criam_tabelas_e_historicos_separados()
    {
        await using var container = new PostgreSqlBuilder("postgres:18.4-alpine3.23")
            .Build();
        await container.StartAsync(TestContext.Current.CancellationToken);

        var services = new ServiceCollection();
        services.AddTenancyModule(container.GetConnectionString());
        services.AddIdentityModule(container.GetConnectionString());

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenancy = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        await identity.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await tenancy.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var tables = await identity.Database
            .SqlQueryRaw<string>("select table_name as \"Value\" from information_schema.tables where table_schema = 'iam'")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains("asp_net_users", tables);
        Assert.Contains("tenants", tables);
        Assert.Contains("__ef_migrations_history", tables);
        Assert.Contains("__ef_migrations_history_identity", tables);
        Assert.DoesNotContain("asp_net_roles", tables);
    }

    [Fact]
    public void Api_registra_health_checks_separados_para_tenancy_e_identity()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:WhatsEnvio"] = "Host=localhost;Database=whatsenvio;Username=whatsenvio;Password=test"
        });

        builder.AddConfigurations();
        builder.AddDbContext();

        using var provider = builder.Services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations
            .Select(registration => registration.Name)
            .ToArray();

        Assert.Contains("tenancy-db", registrations);
        Assert.Contains("identity-db", registrations);
    }
}

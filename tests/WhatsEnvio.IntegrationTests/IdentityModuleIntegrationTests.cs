using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Modules.Identity;
using WhatsEnvio.Modules.Identity.Persistence;
using WhatsEnvio.Modules.Tenancy;
using WhatsEnvio.Modules.Tenancy.Persistence;

namespace WhatsEnvio.IntegrationTests;

public class IdentityModuleIntegrationTests
{
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

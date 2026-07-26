using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WhatsEnvio.Modules.Tenancy.Infrastructure;
using WhatsEnvio.Modules.Tenancy.Infrastructure.Context;

namespace WhatsEnvio.IntegrationTests;

public class PostgresIntegrationTest
{
    [Fact]
    public async Task Migracao_aplica_em_banco_vazio()
    {
        await using var container = new PostgreSqlBuilder("postgres:18.4-alpine3.23")
            .Build();

        await container.StartAsync(TestContext.Current.CancellationToken);

        var service = new ServiceCollection();

        service.AddTenancyModule(container.GetConnectionString());

        await using var provider = service.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var tabelas = await context.Database
        .SqlQueryRaw<string>("select table_name from information_schema.tables where table_schema = 'iam'")
        .ToListAsync(TestContext.Current.CancellationToken);

        var colunas = await context.Database
            .SqlQueryRaw<string>(
                "select column_name from information_schema.columns " +
                "where table_schema = 'iam' and table_name = 'tenants'")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains("tenants", tabelas);
        Assert.Contains("__ef_migrations_history", tabelas);
        Assert.Contains("id", colunas);
        Assert.Contains("name", colunas);
        Assert.Contains("time_zone_id", colunas);
        Assert.Contains("created_at_utc", colunas);
    }
}

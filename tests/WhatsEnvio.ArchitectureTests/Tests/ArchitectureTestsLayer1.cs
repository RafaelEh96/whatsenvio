using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using WhatsEnvio.ArchitectureTests.Extensions;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace WhatsEnvio.ArchitectureTests.Tests;

public class ArchitectureTestsLayer1
{
    private static readonly Architecture _architecture = new ArchLoader()
        .LoadFilteredDirectory(AppContext.BaseDirectory, "WhatsEnvio.*.dll", SearchOption.TopDirectoryOnly)
        .LoadAssemblies(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly, typeof(Npgsql.NpgsqlConnection).Assembly)
        .Build();

    [Fact]
    public void Model_nao_depende_de_orm()
    {
        var models = Types()
            .That().ResideInNamespaceMatching(@"^WhatsEnvio\.Modules\..*\.Model(\..*)?$")
            .NaoVazia(_architecture, "tipos de modelo puro dos módulos");
        var efCore = Types()
            .That().ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore(\..*)?$")
            .NaoVazia(_architecture, "EF Core carregado no ArchLoader");

        IArchRule rule = models.Should().NotDependOnAny(efCore)
            .Because("Modelos e regras do módulo permanecem independentes de persistência");
        rule.Check(_architecture);
    }

    [Fact]
    public void Model_nao_depende_de_npgsql()
    {
        var models = Types()
            .That().ResideInNamespaceMatching(@"^WhatsEnvio\.Modules\..*\.Model(\..*)?$")
            .NaoVazia(_architecture, "tipos de modelo puro dos módulos");
        var npgsql = Types()
            .That().ResideInNamespaceMatching(@"^Npgsql(\..*)?$")
            .NaoVazia(_architecture, "Npgsql carregado no ArchLoader");

        IArchRule rule = models.Should().NotDependOnAny(npgsql)
            .Because("Modelos e regras do módulo permanecem independentes do provider de banco");
        rule.Check(_architecture);
    }
}

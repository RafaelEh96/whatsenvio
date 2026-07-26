using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace WhatsEnvio.ArchitectureTests.Tests;

public class ArchitectureTestsLayer1
{
    private static readonly Architecture _architecture = new ArchLoader()
        .LoadFilteredDirectory(AppContext.BaseDirectory, "WhatsEnvio.*.dll", SearchOption.TopDirectoryOnly)
        .LoadAssemblies(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly, typeof(Npgsql.NpgsqlConnection).Assembly)
        .Build();

    [Fact]
    public void Dominio_nao_depende_de_orm()
    {
        IArchRule regra = Types()
            .That().ResideInNamespaceMatching(@"^WhatsEnvio\.Modules\.Tenancy\.Domain(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore(\..*)?$"))
            .Because("o domínio precisa permanecer independente de persistência");

        regra.Check(_architecture);
    }

    [Fact]
    public void Dominio_nao_depende_de_npgsql()
    {
        IArchRule regra = Types()
            .That().ResideInNamespaceMatching(@"^WhatsEnvio\.Modules\.Tenancy\.Domain(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"^Npgsql(\..*)?$"))
            .Because("o domínio precisa permanecer independente de persistência");
        regra.Check(_architecture);
    }
}

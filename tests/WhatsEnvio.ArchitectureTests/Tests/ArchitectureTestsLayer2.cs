using WhatsEnvio.ArchitectureTests.Extensions;
using WhatsEnvio.ArchitectureTests.Rules;

namespace WhatsEnvio.ArchitectureTests.Tests;

public class ArchitectureTestsLayer2
{
    public static TheoryData<string> ProjectsReferences => [.. ProjectReferenceRule.AllowedReferences.Keys];

    [Theory]
    [MemberData(nameof(ProjectsReferences))]
    public void Deve_ter_apenas_referencias_permitidas(string projectName)
    {
        var expectedReferences = ProjectReferenceRule.AllowedReferences[projectName].OrderBy(x => x);

        var actualReferences = RepositoryExtensions.ProjectReferencesOf(projectName).OrderBy(x => x);

        Assert.Equal(expectedReferences, actualReferences);
    }

    [Theory]
    [InlineData("WhatsEnvio.Api")]
    [InlineData("WhatsEnvio.Worker")]
    [InlineData("WhatsEnvio.Modules.Tenancy.Domain")]
    public void Host_nao_referencia_orm(string projectName)
    {
        var referenciasProibidas = RepositoryExtensions.PackageReferencesOf(projectName)
            .Where(x => x.Contains("EntityFrameworkCore", StringComparison.InvariantCultureIgnoreCase)
            || x.Contains("Npgsql", StringComparison.InvariantCultureIgnoreCase)).ToArray();

        Assert.Empty(referenciasProibidas);
    }
}

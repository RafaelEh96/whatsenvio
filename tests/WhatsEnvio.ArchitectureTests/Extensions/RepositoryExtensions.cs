using System.Xml.Linq;

namespace WhatsEnvio.ArchitectureTests.Extensions;

public class RepositoryExtensions
{
    private static readonly string _repositoryRoot = FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("*.slnx").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Raiz do repositório não encontrada acima do assembly de teste.");
    }

    public static IEnumerable<string> ProjectReferencesOf(string projectName)
    {
        var csproj = Directory
            .EnumerateFiles(_repositoryRoot, $"{projectName}.csproj", SearchOption.AllDirectories)
            .Single(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

        return XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension(x.Attribute("Include")!.Value));
    }

    public static IEnumerable<string> PackageReferencesOf(string projectName)
    {
        var csproj = Directory
            .EnumerateFiles(_repositoryRoot, $"{projectName}.csproj", SearchOption.AllDirectories)
            .Single(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
        return XDocument.Load(csproj)
            .Descendants("PackageReference")
            .Select(x => x.Attribute("Include")!.Value);
    }
}

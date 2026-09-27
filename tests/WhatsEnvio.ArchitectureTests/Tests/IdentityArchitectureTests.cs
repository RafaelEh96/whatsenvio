using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using WhatsEnvio.Modules.Identity.Infrastructure.Context;
using WhatsEnvio.Modules.Identity.Infrastructure.Models;

namespace WhatsEnvio.ArchitectureTests.Tests;

public class IdentityArchitectureTests
{
    [Theory]
    [InlineData("WhatsEnvio.Modules.Identity.Contracts")]
    [InlineData("WhatsEnvio.Modules.Identity.Domain")]
    [InlineData("WhatsEnvio.Modules.Identity.Application")]
    public void Camadas_de_contrato_e_dominio_nao_dependem_do_AspNetIdentity(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var identityReferences = assembly.GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Empty(identityReferences);
        Assert.Equal("WhatsEnvio.Modules.Identity.Infrastructure.Models", typeof(AppUser).Namespace);
    }

    [Fact]
    public void IdentityDbContext_mapeia_usuarios_sem_tabelas_de_roles()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=identity_tests;Username=test;Password=test")
            .Options;
        using var context = new IdentityDbContext(options);

        var entityTypes = context.Model.GetEntityTypes().Select(entity => entity.ClrType).ToArray();

        Assert.Contains(typeof(AppUser), entityTypes);
        Assert.DoesNotContain(typeof(IdentityRole<Guid>), entityTypes);
    }
}

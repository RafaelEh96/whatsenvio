using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatsEnvio.Modules.Identity.Persistence;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.ArchitectureTests.Tests;

public class IdentityArchitectureTests
{
    [Fact]
    public void AppUser_usa_namespace_funcional_de_persistencia()
    {
        Assert.Equal("WhatsEnvio.Modules.Identity.Persistence.Users", typeof(AppUser).Namespace);
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

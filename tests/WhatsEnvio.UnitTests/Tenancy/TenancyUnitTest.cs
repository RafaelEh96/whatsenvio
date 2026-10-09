namespace WhatsEnvio.UnitTests.Tenancy;

public class TenancyUnitTest
{
    [Fact]
    public void Deve_criar_um_tenant_com_sucesso()
    {
        // Arrange
        var name = "Tenant Teste";
        var timeZoneId = "America/Sao_Paulo";
        var createdAtUtc = DateTimeOffset.UtcNow;
        // Act
        var tenant = new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc);
        // Assert
        Assert.Equal(name, tenant.Name);
        Assert.Equal(timeZoneId, tenant.TimeZoneId);
        Assert.Equal(createdAtUtc, tenant.CreatedAtUtc);
        Assert.True(tenant.IsActive);
    }
    
    [Fact]
    public void Deve_lancar_excecao_quando_nome_for_nulo_ou_vazio()
    {
        // Arrange
        var name = "";
        var timeZoneId = "America/Sao_Paulo";
        var createdAtUtc = DateTimeOffset.UtcNow;
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc));
    }

    [Fact]
    public void Deve_lancar_excecao_quando_fuso_horario_for_nulo_ou_vazio()
    {
        // Arrange
        var name = "Tenant Teste";
        var timeZoneId = "";
        var createdAtUtc = DateTimeOffset.UtcNow;
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc));
    }

    [Fact]
    public void Deve_lancar_excecao_quando_fuso_horario_nao_existir()
    {
        // Arrange
        var name = "Tenant Teste";
        var timeZoneId = "FusoHorárioInexistente";
        var createdAtUtc = DateTimeOffset.UtcNow;
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc));
    }

    [Fact]
    public void Deve_desativar_tenant_com_sucesso()
    {
        // Arrange
        var name = "Tenant Teste";
        var timeZoneId = "America/Sao_Paulo";
        var createdAtUtc = DateTimeOffset.UtcNow;
        var tenant = new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc);
        // Act
        tenant.Deactivate();
        // Assert
        Assert.False(tenant.IsActive);
    }
    
    [Fact]
    public void Deve_lancar_excecao_quando_tentar_desativar_tenant_inativo()
    {
        // Arrange
        var name = "Tenant Teste";
        var timeZoneId = "America/Sao_Paulo";
        var createdAtUtc = DateTimeOffset.UtcNow;
        var tenant = new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc);
        tenant.Deactivate();
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tenant.Deactivate());
    }

    [Fact]
    public void Deve_criar_um_tenant_com_sucesso_e_validar_fuso_horario()
    {
        // Arrange
        var name = "Tenant Teste";
        var timeZoneId = "America/Sao_Paulo";
        var createdAtUtc = DateTimeOffset.UtcNow;
        // Act
        var tenant = new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc);
        // Assert
        Assert.Equal(name, tenant.Name);
        Assert.Equal(timeZoneId, tenant.TimeZoneId);
        Assert.Equal(createdAtUtc, tenant.CreatedAtUtc);
        Assert.True(tenant.IsActive);
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        Assert.NotNull(timeZoneInfo);
    }

    [Fact]
    public void Deve_lancar_excecao_quando_nome_for_nulo_ou_espaco()
    {
        // Arrange
        var name = "   ";
        var timeZoneId = "America/Sao_Paulo";
        var createdAtUtc = DateTimeOffset.UtcNow;
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc));
    }

    [Fact]
    public void Deve_remover_espacos_em_branco_do_nome_do_tenant()
    {
        // Arrange
        var name = "   Tenant Teste   ";
        var timeZoneId = "America/Sao_Paulo";
        var createdAtUtc = DateTimeOffset.UtcNow;
        // Act
        var tenant = new WhatsEnvio.Modules.Tenancy.Tenants.Model.Tenant(name, timeZoneId, createdAtUtc);
        // Assert
        Assert.Equal("Tenant Teste", tenant.Name);
    }
}

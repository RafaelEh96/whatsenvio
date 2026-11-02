using WhatsEnvio.Core.Tenancy;

namespace WhatsEnvio.UnitTests;

public class TenantContextAccessorTests
{
    [Fact]
    public async Task Escopo_preserva_contexto_apos_await_e_restaura_valor_anterior()
    {
        var accessor = new TenantContextAccessor();
        var outer = Guid.NewGuid();
        var inner = Guid.NewGuid();
        Assert.Null(accessor.TenantId);
        using (accessor.BeginScope(outer))
        {
            await Task.Yield();
            Assert.Equal(outer, accessor.TenantId);
            Assert.Throws<InvalidOperationException>((Action)(() =>
            {
                using var scope = accessor.BeginScope(inner);
                Assert.Equal(inner, accessor.TenantId);
                throw new InvalidOperationException();
            }));
            Assert.Equal(outer, accessor.TenantId);
        }
        Assert.Null(accessor.TenantId);
    }

    [Fact]
    public async Task Fluxos_concorrentes_mantem_tenants_separados()
    {
        var accessor = new TenantContextAccessor();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        async Task<Guid?> Run(Guid id)
        {
            using var scope = accessor.BeginScope(id);
            if (Interlocked.Increment(ref count) == 2) ready.SetResult();
            await ready.Task;
            await Task.Yield();
            return accessor.TenantId;
        }
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var results = await Task.WhenAll(Run(first), Run(second));
        Assert.Equal(new Guid?[] { first, second }, results);
        Assert.Null(accessor.TenantId);
    }

    [Fact]
    public void Escopo_rejeita_id_vazio()
    {
        var accessor = new TenantContextAccessor();
        Assert.Throws<ArgumentException>(() => accessor.BeginScope(Guid.Empty));
    }
}

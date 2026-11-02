using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Core.Dtos.Membership;
using WhatsEnvio.Core.Dtos.Tenancy;
using WhatsEnvio.Core.Enums;
using WhatsEnvio.Core.Tenancy;
using WhatsEnvio.Modules.Tenancy.Contracts;

namespace WhatsEnvio.IntegrationTests;

public class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task Rota_marcada_valida_vinculo_e_limpa_o_contexto()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var accessor = new TenantContextAccessor();
        var context = CreateContext(userId, tenantId.ToString());
        var called = false;
        var middleware = new TenantResolutionMiddleware(async ctx =>
        {
            called = true;
            await Task.Yield();
            Assert.Equal(tenantId, accessor.TenantId);
        });
        await middleware.InvokeAsync(context, new MembershipServiceStub(new(tenantId, userId, MembershipRole.Admin)), accessor);
        Assert.True(called);
        Assert.Null(accessor.TenantId);
    }

    [Fact]
    public async Task Rota_marcada_recusa_ausencia_de_vinculo()
    {
        var context = CreateContext(Guid.NewGuid(), Guid.NewGuid().ToString());
        var middleware = new TenantResolutionMiddleware(_ => throw new InvalidOperationException("Endpoint não deveria executar"));
        await middleware.InvokeAsync(context, new MembershipServiceStub(null), new TenantContextAccessor());
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task Rota_marcada_recusa_tenant_invalido()
    {
        var context = CreateContext(Guid.NewGuid(), "invalido");
        var middleware = new TenantResolutionMiddleware(_ => throw new InvalidOperationException("Endpoint não deveria executar"));
        await middleware.InvokeAsync(context, new MembershipServiceStub(null), new TenantContextAccessor());
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Rota_nao_marcada_nao_exige_tenant()
    {
        var context = new DefaultHttpContext();
        var called = false;
        var middleware = new TenantResolutionMiddleware(_ => { called = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, new MembershipServiceStub(null), new TenantContextAccessor());
        Assert.True(called);
    }

    [Fact]
    public async Task Contexto_e_restaurado_quando_endpoint_falha()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var accessor = new TenantContextAccessor();
        var context = CreateContext(userId, tenantId.ToString());
        var middleware = new TenantResolutionMiddleware(_ => throw new InvalidOperationException());
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(
            context, new MembershipServiceStub(new(tenantId, userId, MembershipRole.Admin)), accessor));
        Assert.Null(accessor.TenantId);
    }

    [Fact]
    public async Task Rota_marcada_recusa_usuario_anonimo()
    {
        var context = CreateContext(Guid.NewGuid(), Guid.NewGuid().ToString());
        context.User = new ClaimsPrincipal();
        var middleware = new TenantResolutionMiddleware(_ => throw new InvalidOperationException());
        await middleware.InvokeAsync(context, new MembershipServiceStub(null), new TenantContextAccessor());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext(Guid userId, string tenantId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
        context.Request.RouteValues["tenantId"] = tenantId;
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequireTenantMetadata()), "test"));
        return context;
    }

    private sealed class MembershipServiceStub(MembershipResult? result) : IMembershipService
    {
        public Task<MembershipResult?> GetActiveAsync(Guid userId, Guid tenantId, CancellationToken token = default) => Task.FromResult(result);
        public Task<IReadOnlyList<TenantSummary>> ListActiveAsync(Guid userId, CancellationToken token = default) => throw new NotSupportedException();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WhatsEnvio.Api.Authorization;
using WhatsEnvio.Core.Enums;
using WhatsEnvio.Core.Tenancy;

namespace WhatsEnvio.IntegrationTests.Authorization;

public sealed class TenantAuthorizationMiddlewareResultHandlerTests
{
    private static readonly AuthorizationPolicy TenantPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().AddRequirements(new TenantRequirement(MembershipRole.Owner)).Build();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Contexto_envolve_endpoint_apos_await_e_restaura_escopo_anterior(bool endpointFails)
    {
        var outerTenant = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var state = new TenantAuthorizationState();
        state.StoreMembership(userId, tenantId, new(tenantId, userId, MembershipRole.Owner));
        var accessor = new TenantContextAccessor();
        var handler = CreateHandler(state, accessor);
        var http = TenantAuthorizationHandlerTests.CreateHttpContext(userId.ToString(), tenantId.ToString());
        var called = false;
        using (accessor.BeginScope(outerTenant))
        {
            async Task Next(HttpContext _)
            {
                called = true;
                await Task.Yield();
                Assert.Equal(tenantId, accessor.TenantId);
                if (endpointFails) throw new InvalidOperationException("Falha simulada");
            }

            if (endpointFails)
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    handler.HandleAsync(Next, http, TenantPolicy, PolicyAuthorizationResult.Success()));
            else
                await handler.HandleAsync(Next, http, TenantPolicy, PolicyAuthorizationResult.Success());

            Assert.True(called);
            Assert.Equal(outerTenant, accessor.TenantId);
        }
        Assert.Null(accessor.TenantId);
    }

    [Fact]
    public async Task Requisicoes_concorrentes_nao_compartilham_tenant()
    {
        var accessor = new TenantContextAccessor();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        async Task<Guid?> Run(Guid tenantId)
        {
            var userId = Guid.NewGuid();
            var state = new TenantAuthorizationState();
            state.StoreMembership(userId, tenantId, new(tenantId, userId, MembershipRole.Owner));
            var http = TenantAuthorizationHandlerTests.CreateHttpContext(userId.ToString(), tenantId.ToString());
            Guid? observed = null;
            await CreateHandler(state, accessor).HandleAsync(async _ =>
            {
                if (Interlocked.Increment(ref count) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                await Task.Yield();
                observed = accessor.TenantId;
            }, http, TenantPolicy, PolicyAuthorizationResult.Success());
            Assert.Null(accessor.TenantId);
            return observed;
        }

        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var observed = await Task.WhenAll(Run(first), Run(second));

        Assert.Equal(new Guid?[] { first, second }, observed);
        Assert.Null(accessor.TenantId);
    }

    [Theory]
    [InlineData("ausente")]
    [InlineData("nulo")]
    [InlineData("usuario")]
    [InlineData("tenant")]
    [InlineData("rota_invalida")]
    [InlineData("claim_invalida")]
    public async Task Sucesso_sem_membership_consistente_e_recusado(string scenario)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var state = new TenantAuthorizationState();
        if (scenario != "ausente")
            state.StoreMembership(userId, tenantId, scenario == "nulo" ? null : new(
                scenario == "tenant" ? Guid.NewGuid() : tenantId,
                scenario == "usuario" ? Guid.NewGuid() : userId, MembershipRole.Owner));
        var http = TenantAuthorizationHandlerTests.CreateHttpContext(
            scenario == "claim_invalida" ? "invalido" : userId.ToString(),
            scenario == "rota_invalida" ? "invalido" : tenantId.ToString());
        // Real authentication/forbid pipeline; no substitute authorization handler.
        var services = new ServiceCollection().AddLogging();
        services.AddAuthentication("test").AddCookie("test", options =>
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        http.RequestServices = scope.ServiceProvider;
        var accessor = new TenantContextAccessor();

        await CreateHandler(state, accessor).HandleAsync(
            _ => throw new InvalidOperationException("Endpoint não deveria executar"),
            http, TenantPolicy, PolicyAuthorizationResult.Success());

        Assert.Equal(StatusCodes.Status403Forbidden, http.Response.StatusCode);
        Assert.Null(accessor.TenantId);
    }

    [Fact]
    public async Task Policy_sem_tenant_executa_endpoint_sem_abrir_contexto()
    {
        var accessor = new TenantContextAccessor();
        var called = false;
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        await CreateHandler(new(), accessor).HandleAsync(_ =>
        {
            called = true;
            Assert.Null(accessor.TenantId);
            return Task.CompletedTask;
        }, new DefaultHttpContext(), policy, PolicyAuthorizationResult.Success());

        Assert.True(called);
        Assert.Null(accessor.TenantId);
    }

    private static TenantAuthorizationMiddlewareResultHandler CreateHandler(
        TenantAuthorizationState state, ITenantContextAccessor accessor)
        => new(state, accessor, NullLogger<TenantAuthorizationMiddlewareResultHandler>.Instance);
}

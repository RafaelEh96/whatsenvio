using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using WhatsEnvio.Api.Authorization;
using WhatsEnvio.Core.Dtos.Membership;
using WhatsEnvio.Core.Dtos.Tenancy;
using WhatsEnvio.Core.Enums;
using WhatsEnvio.Modules.Tenancy.Contracts;

namespace WhatsEnvio.IntegrationTests.Authorization;

public sealed class TenantAuthorizationHandlerTests
{
    [Theory]
    [InlineData(null, "11111111-1111-1111-1111-111111111111")]
    [InlineData("invalido", "11111111-1111-1111-1111-111111111111")]
    [InlineData("00000000-0000-0000-0000-000000000000", "11111111-1111-1111-1111-111111111111")]
    [InlineData("11111111-1111-1111-1111-111111111111", null)]
    [InlineData("11111111-1111-1111-1111-111111111111", "invalido")]
    [InlineData("11111111-1111-1111-1111-111111111111", "00000000-0000-0000-0000-000000000000")]
    public async Task Identificadores_invalidos_nao_autorizam_nem_consultam_membership(string? userId, string? tenantId)
    {
        var service = new MembershipProbe((_, _, _) => throw new InvalidOperationException("Não deveria consultar"));
        var http = CreateHttpContext(userId, tenantId);
        var authorization = CreateAuthorization(http);

        await new TenantAuthorizationHandler(service, new()).HandleAsync(authorization);

        Assert.False(authorization.HasSucceeded);
        Assert.Empty(service.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Usuario_anonimo_ou_resource_sem_http_nao_autoriza(bool anonymous)
    {
        var service = new MembershipProbe((_, _, _) => throw new InvalidOperationException("Não deveria consultar"));
        var http = CreateHttpContext(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        if (anonymous) http.User = new ClaimsPrincipal();
        var authorization = CreateAuthorization(http, anonymous ? http : new object());

        await new TenantAuthorizationHandler(service, new()).HandleAsync(authorization);

        Assert.False(authorization.HasSucceeded);
        Assert.Empty(service.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Policies_acumuladas_reutilizam_consulta_inclusive_ausencia_de_vinculo(bool hasMembership)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var service = new MembershipProbe((user, tenant, _) => hasMembership
            ? new MembershipResult(tenant, user, MembershipRole.Admin) : null);
        var http = CreateHttpContext(userId.ToString(), tenantId.ToString());
        http.RequestAborted = cancellation.Token;
        var state = new TenantAuthorizationState();
        var handler = new TenantAuthorizationHandler(service, state);
        var member = CreateAuthorization(http);
        var admin = new AuthorizationHandlerContext(
            [new TenantRequirement(MembershipRole.Owner, MembershipRole.Admin)], http.User, http);

        await handler.HandleAsync(member);
        await handler.HandleAsync(admin);

        Assert.Equal(hasMembership, member.HasSucceeded);
        Assert.Equal(hasMembership, admin.HasSucceeded);
        var call = Assert.Single(service.Calls);
        Assert.Equal((userId, tenantId, cancellation.Token), call);
        Assert.True(state.TryGetMembership(userId, tenantId, out var stored));
        Assert.Equal(hasMembership, stored is not null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cache_nao_reutiliza_vinculo_de_outro_usuario_ou_tenant(bool changeUser)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var state = new TenantAuthorizationState();
        state.StoreMembership(userId, tenantId, new(tenantId, userId, MembershipRole.Owner));
        var service = new MembershipProbe((_, _, _) => null);
        var requestedUser = changeUser ? Guid.NewGuid() : userId;
        var requestedTenant = changeUser ? tenantId : Guid.NewGuid();
        var authorization = CreateAuthorization(CreateHttpContext(requestedUser.ToString(), requestedTenant.ToString()));

        await new TenantAuthorizationHandler(service, state).HandleAsync(authorization);

        Assert.False(authorization.HasSucceeded);
        var call = Assert.Single(service.Calls);
        Assert.Equal(requestedUser, call.UserId);
        Assert.Equal(requestedTenant, call.TenantId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Membership_de_outro_usuario_ou_tenant_nao_autoriza(bool wrongUser)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var service = new MembershipProbe((_, _, _) => new(
            wrongUser ? tenantId : Guid.NewGuid(),
            wrongUser ? Guid.NewGuid() : userId,
            MembershipRole.Owner));
        var authorization = CreateAuthorization(CreateHttpContext(userId.ToString(), tenantId.ToString()));

        await new TenantAuthorizationHandler(service, new()).HandleAsync(authorization);

        Assert.False(authorization.HasSucceeded);
    }

    [Fact]
    public async Task Falha_de_infraestrutura_nao_e_convertida_em_ausencia_de_vinculo()
    {
        var expected = new InvalidOperationException("Banco indisponível");
        var service = new MembershipProbe((_, _, _) => throw expected);
        var authorization = CreateAuthorization(CreateHttpContext(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TenantAuthorizationHandler(service, new()).HandleAsync(authorization));

        Assert.Same(expected, exception);
    }

    internal static DefaultHttpContext CreateHttpContext(string? userId, string? tenantId)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            userId is null ? [] : [new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        http.Request.RouteValues["tenantId"] = tenantId;
        return http;
    }

    private static AuthorizationHandlerContext CreateAuthorization(HttpContext http, object? resource = null)
        => new([new TenantRequirement(MembershipRole.Owner, MembershipRole.Admin, MembershipRole.Professional)],
            http.User, resource ?? http);

    private sealed class MembershipProbe(Func<Guid, Guid, CancellationToken, MembershipResult?> resolve) : IMembershipService
    {
        public List<(Guid UserId, Guid TenantId, CancellationToken Token)> Calls { get; } = [];

        public Task<MembershipResult?> GetActiveAsync(Guid userId, Guid tenantId, CancellationToken token = default)
        {
            Calls.Add((userId, tenantId, token));
            return Task.FromResult(resolve(userId, tenantId, token));
        }

        public Task<IReadOnlyList<TenantSummary>> ListActiveAsync(Guid userId, CancellationToken token = default)
            => throw new NotSupportedException();
    }
}

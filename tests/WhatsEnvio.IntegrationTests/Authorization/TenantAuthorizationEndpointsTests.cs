using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WhatsEnvio.Api.Authorization;
using WhatsEnvio.Core.Enums;
using WhatsEnvio.Core.Observability;
using WhatsEnvio.Core.Tenancy;
using WhatsEnvio.Modules.Tenancy.Persistence.Context;

namespace WhatsEnvio.IntegrationTests.Authorization;

public sealed class TenantAuthorizationEndpointsTests
{
    [Theory]
    [InlineData(MembershipRole.Professional, "member", HttpStatusCode.OK)]
    [InlineData(MembershipRole.Professional, "admin", HttpStatusCode.Forbidden)]
    [InlineData(MembershipRole.Professional, "owner", HttpStatusCode.Forbidden)]
    [InlineData(MembershipRole.Admin, "member", HttpStatusCode.OK)]
    [InlineData(MembershipRole.Admin, "admin", HttpStatusCode.OK)]
    [InlineData(MembershipRole.Admin, "owner", HttpStatusCode.Forbidden)]
    [InlineData(MembershipRole.Owner, "member", HttpStatusCode.OK)]
    [InlineData(MembershipRole.Owner, "admin", HttpStatusCode.OK)]
    [InlineData(MembershipRole.Owner, "owner", HttpStatusCode.OK)]
    public async Task Cookie_real_respeita_funcao_do_membership_no_tenant_da_rota(
        MembershipRole role, string endpoint, HttpStatusCode expected)
    {
        using var factory = new TenantApiFactory();
        var data = await factory.SeedAsync();
        using var client = factory.CreateHttpsClient();
        await TenantApiFactory.LoginAsync(client);
        var tenantId = role switch
        {
            MembershipRole.Professional => data.Professional,
            MembershipRole.Admin => data.Admin,
            MembershipRole.Owner => data.Owner,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };

        using var response = await client.GetAsync(Path(tenantId, endpoint), TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<TenantResponse>(TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.Equal(tenantId, result.TenantId);
        }
    }

    [Theory]
    [InlineData("member")]
    [InlineData("admin")]
    [InlineData("owner")]
    public async Task Sem_cookie_retorna_401_sem_redirecionamento(string endpoint)
    {
        using var factory = new TenantApiFactory();
        var data = await factory.SeedAsync();
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync(Path(data.Owner, endpoint), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("member")]
    [InlineData("admin")]
    [InlineData("owner")]
    public async Task Sem_vinculo_inexistente_e_inativos_retornam_o_mesmo_403(string endpoint)
    {
        using var factory = new TenantApiFactory();
        var data = await factory.SeedAsync();
        using var client = factory.CreateHttpsClient();
        await TenantApiFactory.LoginAsync(client);
        string[]? expectedProblem = null;
        foreach (var tenantId in new[] { data.Other, Guid.NewGuid(), data.InactiveTenant, data.InactiveMembership, Guid.Empty })
        {
            using var response = await client.GetAsync(Path(tenantId, endpoint), TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            using var body = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);
            var root = body.RootElement;
            Assert.Equal(403, root.GetProperty("status").GetInt32());
            // Only request-specific identifiers may differ, never details that reveal tenant existence.
            var problem = root.EnumerateObject()
                .Where(property => property.Name is not "traceId" and not "instance")
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => $"{property.Name}:{property.Value.GetRawText()}")
                .ToArray();
            if (expectedProblem is null) expectedProblem = problem;
            else Assert.Equal(expectedProblem, problem);
        }
    }

    [Fact]
    public async Task Cache_de_membership_nao_sobrevive_entre_requisicoes()
    {
        using var factory = new TenantApiFactory();
        var data = await factory.SeedAsync();
        using var client = factory.CreateHttpsClient();
        await TenantApiFactory.LoginAsync(client);
        var token = TestContext.Current.CancellationToken;
        using var allowed = await client.GetAsync(Path(data.Owner, "owner"), token);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        var membership = await db.Memberships.SingleAsync(item => item.TenantId == data.Owner, token);
        membership.IsActive = false;
        await db.SaveChangesAsync(token);

        using var denied = await client.GetAsync(Path(data.Owner, "owner"), token);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        membership.IsActive = true;
        await db.SaveChangesAsync(token);

        using var reactivated = await client.GetAsync(Path(data.Owner, "owner"), token);
        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
    }

    [Fact]
    public async Task Rotas_de_tenant_preservam_policy_member_e_nao_permitem_anonimo()
    {
        using var factory = new TenantApiFactory();
        _ = factory.Server;
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/v1/tenants/", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(endpoints);
        foreach (var endpoint in endpoints)
        {
            Assert.Null(endpoint.Metadata.GetMetadata<IAllowAnonymous>());
            Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                metadata => metadata.Policy == TenantPolicies.Member);
        }
        foreach (var suffix in new[] { "/member", "/admin", "/owner" })
            Assert.Contains(endpoints, endpoint => endpoint.RoutePattern.RawText!.EndsWith(suffix, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cada_requisicao_autorizada_registra_tenant_no_log_estruturado_e_no_trace_http()
    {
        using var factory = new TenantApiFactory();
        var data = await factory.SeedAsync();
        using var client = factory.CreateHttpsClient();
        await TenantApiFactory.LoginAsync(client);
        foreach (var (tenantId, endpoint) in new[] { (data.Owner, "owner"), (data.Admin, "admin"), (data.Professional, "member") })
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var stopped = new TaskCompletionSource<Activity>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var listener = ListenForRequest(correlationId, stopped);
            using var request = new HttpRequestMessage(HttpMethod.Get, Path(tenantId, endpoint));
            request.Headers.Add(CorrelationId.HeaderName, correlationId);

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var activity = await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.Equal(ActivityKind.Server, activity.Kind);
            Assert.Equal(tenantId.ToString(), activity.GetTagItem("tenant.id"));
            var log = Assert.Single(factory.LogProvider.Entries, entry =>
                entry.Category == typeof(TenantAuthorizationMiddlewareResultHandler).FullName
                && entry.Scope.TryGetValue("CorrelationId", out var value)
                && Equals(value, correlationId));
            Assert.Equal(tenantId, log.Scope["TenantId"]);
            Assert.Equal(tenantId, log.Properties["TenantId"]);
        }
        Assert.Null(factory.Services.GetRequiredService<ITenantContext>().TenantId);
    }

    [Fact]
    public async Task Recusa_e_rota_sem_tenant_nao_herdam_contexto_ou_tag_da_requisicao_anterior()
    {
        using var factory = new TenantApiFactory();
        var data = await factory.SeedAsync();
        using var client = factory.CreateHttpsClient();
        await TenantApiFactory.LoginAsync(client);
        using var allowed = await client.GetAsync(Path(data.Owner, "owner"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        foreach (var (path, expected) in new[]
        {
            (Path(data.Other, "member"), HttpStatusCode.Forbidden),
            (Path(data.Professional, "admin"), HttpStatusCode.Forbidden),
            ("/api/v1/auth/csrf", HttpStatusCode.NoContent)
        })
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var stopped = new TaskCompletionSource<Activity>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var listener = ListenForRequest(correlationId, stopped);
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add(CorrelationId.HeaderName, correlationId);
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(expected, response.StatusCode);
            var activity = await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Null(activity.GetTagItem("tenant.id"));
            Assert.DoesNotContain(factory.LogProvider.Entries, entry =>
                entry.Scope.TryGetValue("CorrelationId", out var value) && Equals(value, correlationId)
                && (entry.Scope.ContainsKey("TenantId") || entry.Properties.ContainsKey("TenantId")));
        }
        Assert.Null(factory.Services.GetRequiredService<ITenantContext>().TenantId);
    }

    private static ActivityListener ListenForRequest(string correlationId, TaskCompletionSource<Activity> stopped)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (Equals(activity.GetTagItem(CorrelationId.TagName), correlationId)) stopped.TrySetResult(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static string Path(Guid tenantId, string endpoint) => $"/api/v1/tenants/{tenantId}/{endpoint}";
    private sealed record TenantResponse(Guid? TenantId);
}

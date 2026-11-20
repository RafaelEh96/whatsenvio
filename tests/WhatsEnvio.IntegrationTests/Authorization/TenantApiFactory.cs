using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WhatsEnvio.Core.Enums;
using WhatsEnvio.Modules.Identity.Persistence.Context;
using WhatsEnvio.Modules.Identity.Persistence.Users;
using WhatsEnvio.Modules.Tenancy.Memberships.Model;
using WhatsEnvio.Modules.Tenancy.Persistence.Context;
using WhatsEnvio.Modules.Tenancy.Tenants.Model;

namespace WhatsEnvio.IntegrationTests.Authorization;

internal sealed class TenantApiFactory : WebApplicationFactory<Program>
{
    private const string Email = "tenant-owner@whatsenvio.test";
    private const string Password = "Correct-Password-123!";
    private readonly string _databaseName = $"tenant-authorization-{Guid.NewGuid():N}";
    public StructuredLogProvider LogProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:WhatsEnvio",
            "Host=localhost;Database=whatsenvio_tests;Username=test;Password=test");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDataProtectionProvider>();
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.RemoveAll<IDbContextOptionsConfiguration<TenancyDbContext>>();
            services.RemoveAll<DbContextOptions<TenancyDbContext>>();
            services.RemoveAll<TenancyDbContext>();
            services.AddDbContext<TenancyDbContext>(options => options.UseInMemoryDatabase(_databaseName + "-tenancy"));
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
            services.AddLogging(logging => logging.AddProvider(LogProvider));
        });
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
        AllowAutoRedirect = false
    });

    public async Task<TenantScenario> SeedAsync()
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { Id = Guid.NewGuid(), UserName = Email, Email = Email };
        var created = await users.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Code)));

        var db = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        var professional = new Tenant("Professional", "UTC", DateTimeOffset.UtcNow);
        var admin = new Tenant("Admin", "UTC", DateTimeOffset.UtcNow);
        var owner = new Tenant("Owner", "UTC", DateTimeOffset.UtcNow);
        var other = new Tenant("Outra empresa", "UTC", DateTimeOffset.UtcNow);
        var inactive = new Tenant("Empresa inativa", "UTC", DateTimeOffset.UtcNow);
        inactive.Deactivate();
        var inactiveMembership = new Tenant("Vínculo inativo", "UTC", DateTimeOffset.UtcNow);
        db.Tenants.AddRange(professional, admin, owner, other, inactive, inactiveMembership);
        db.Memberships.AddRange(
            new Membership(professional.Id, user.Id, true, MembershipRole.Professional),
            new Membership(admin.Id, user.Id, true, MembershipRole.Admin),
            new Membership(owner.Id, user.Id, true, MembershipRole.Owner),
            new Membership(other.Id, Guid.NewGuid(), true, MembershipRole.Owner),
            new Membership(inactive.Id, user.Id, true, MembershipRole.Owner),
            new Membership(inactiveMembership.Id, user.Id, false, MembershipRole.Owner));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new(professional.Id, admin.Id, owner.Id, other.Id, inactive.Id, inactiveMembership.Id);
    }

    public static async Task LoginAsync(HttpClient client)
    {
        var token = TestContext.Current.CancellationToken;
        using var csrf = await client.GetAsync("/api/v1/auth/csrf", token);
        Assert.Equal(HttpStatusCode.NoContent, csrf.StatusCode);
        var cookie = Assert.Single(csrf.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        var csrfToken = Uri.UnescapeDataString(cookie["XSRF-TOKEN=".Length..].Split(';', 2)[0]);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { Email, Password })
        };
        login.Headers.Add("X-XSRF-TOKEN", csrfToken);
        using var response = await client.SendAsync(login, token);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("WhatsEnvio.Auth=", StringComparison.Ordinal));
    }
}

internal sealed record TenantScenario(
    Guid Professional, Guid Admin, Guid Owner, Guid Other, Guid InactiveTenant, Guid InactiveMembership);

internal sealed record StructuredLog(
    string Category,
    IReadOnlyDictionary<string, object?> Properties,
    IReadOnlyDictionary<string, object?> Scope);

internal sealed class StructuredLogProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
    private readonly ConcurrentQueue<StructuredLog> _entries = new();
    public StructuredLog[] Entries => _entries.ToArray();

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;
    public ILogger CreateLogger(string categoryName) => new ProbeLogger(this, categoryName);
    public void Dispose() { }

    private sealed class ProbeLogger(StructuredLogProvider provider, string category) : ILogger
    {
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => provider._scopes.Push(state);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var properties = new Dictionary<string, object?>();
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
                foreach (var pair in values) properties[pair.Key] = pair.Value;
            var scopes = new Dictionary<string, object?>();
            provider._scopes.ForEachScope((scope, target) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> items)
                    foreach (var pair in items) target[pair.Key] = pair.Value;
            }, scopes);
            provider._entries.Enqueue(new(category, properties, scopes));
        }
    }
}

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using WhatsEnvio.Modules.Identity.Application.Interfaces;
using WhatsEnvio.Modules.Identity.Persistence;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.IntegrationTests;

public sealed class AuthEndpointsIntegrationTests
{
    private const string Email = "owner@whatsenvio.test";
    private const string Password = "Correct-Password-123!";
    private const string AuthPath = "/api/v1/auth";
    private const string UserPath = "/api/v1/users";

    [Fact]
    public async Task Forgot_password_token_codificado_pelo_email_pode_redefinir_senha()
    {
        using var factory = new ApiFactory();
        await CreateUserAsync(factory);
        using var client = factory.CreateClient(HttpsClientOptions());
        var emailSender = factory.Services.GetRequiredService<CapturingIdentityEmailSender>();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var forgotResponse = await client.PostAsJsonAsync(
            $"{UserPath}/forgot-password",
            new { Email },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, forgotResponse.StatusCode);
        Assert.NotNull(emailSender.Token);

        var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(emailSender.Token));
        const string newPassword = "New-Correct-Password-123!";
        using var resetResponse = await client.PostAsJsonAsync(
            $"{UserPath}/reset-password",
            new { Email, Token = encodedToken, NewPassword = newPassword },
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var csrfToken = await GetAntiforgeryTokenAsync(client);
        using var loginResponse = await PostWithTokenAsync(
            client,
            $"{AuthPath}/login",
            csrfToken,
            new { Email, Password = newPassword });

        Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Csrf_emite_cookie_legivel_e_login_rejeita_post_sem_header()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(HttpsClientOptions());

        using var csrfResponse = await client.GetAsync($"{AuthPath}/csrf");

        Assert.Equal(HttpStatusCode.NoContent, csrfResponse.StatusCode);
        var cookies = SetCookies(csrfResponse);
        var readableCookie = Assert.Single(cookies, cookie => cookie.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        Assert.Contains("secure", readableCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", readableCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", readableCookie, StringComparison.OrdinalIgnoreCase);

        var internalCookie = Assert.Single(cookies, cookie => !cookie.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        Assert.Contains("httponly", internalCookie, StringComparison.OrdinalIgnoreCase);

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, $"{AuthPath}/login")
        {
            Content = JsonContent.Create(new { Email, Password })
        };
        using var loginResponse = await client.SendAsync(loginRequest);

        Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        Assert.Equal("application/problem+json", loginResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_me_logout_e_cookie_de_autenticacao_funcionam_em_conjunto()
    {
        using var factory = new ApiFactory();
        await CreateUserAsync(factory);
        using var client = factory.CreateClient(HttpsClientOptions());

        var anonymousToken = await GetAntiforgeryTokenAsync(client);
        using var loginResponse = await PostWithTokenAsync(
            client,
            $"{AuthPath}/login",
            anonymousToken,
            new { Email, Password });

        Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);
        var authCookie = Assert.Single(
            SetCookies(loginResponse),
            cookie => cookie.StartsWith("WhatsEnvio.Auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", authCookie, StringComparison.OrdinalIgnoreCase);

        var authenticatedToken = await GetAntiforgeryTokenAsync(client);
        using var meResponse = await client.GetAsync($"{AuthPath}/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var user = await meResponse.Content.ReadFromJsonAsync<UserDetailsResponse>();
        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, Guid.Parse(user.Id));
        Assert.Equal(Email, user.Name);

        using var logoutResponse = await PostWithTokenAsync(
            client,
            $"{AuthPath}/logout",
            authenticatedToken);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var afterLogout = await client.GetAsync($"{AuthPath}/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        Assert.Equal("application/problem+json", afterLogout.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_nao_revela_se_email_existe_e_lockout_bloqueia_apos_cinco_falhas()
    {
        using var factory = new ApiFactory();
        await CreateUserAsync(factory);
        using var client = factory.CreateClient(HttpsClientOptions());
        var token = await GetAntiforgeryTokenAsync(client);

        using var unknownUserResponse = await PostWithTokenAsync(
            client,
            $"{AuthPath}/login",
            token,
            new { Email = "missing@whatsenvio.test", Password });
        using var wrongPasswordResponse = await PostWithTokenAsync(
            client,
            $"{AuthPath}/login",
            token,
            new { Email, Password = "Wrong-Password-123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, unknownUserResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(
            await unknownUserResponse.Content.ReadAsStringAsync(),
            await wrongPasswordResponse.Content.ReadAsStringAsync());

        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var failedAttempt = await PostWithTokenAsync(
                client,
                $"{AuthPath}/login",
                token,
                new { Email, Password = "Wrong-Password-123!" });
            Assert.Equal(HttpStatusCode.Unauthorized, failedAttempt.StatusCode);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(Email);
            Assert.NotNull(user);
            Assert.True(await users.IsLockedOutAsync(user));
        }

        using var correctPasswordWhileLocked = await PostWithTokenAsync(
            client,
            $"{AuthPath}/login",
            token,
            new { Email, Password });
        Assert.Equal(HttpStatusCode.Unauthorized, correctPasswordWhileLocked.StatusCode);
    }

    [Fact]
    public async Task Rota_protegida_sem_cookie_retorna_401_em_problem_details()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(HttpsClientOptions());

        using var response = await client.GetAsync($"{AuthPath}/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Revogar_sessoes_invalida_cookie_de_outra_sessao()
    {
        using var factory = new ApiFactory();
        await CreateUserAsync(factory);
        using var firstClient = factory.CreateClient(HttpsClientOptions());
        using var secondClient = factory.CreateClient(HttpsClientOptions());

        await LoginAsync(firstClient);
        await LoginAsync(secondClient);

        var token = await GetAntiforgeryTokenAsync(firstClient);
        using var revokeResponse = await PostWithTokenAsync(
            firstClient,
            $"{AuthPath}/revoke-sessions",
            token);

        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        using var staleSessionResponse = await secondClient.GetAsync($"{AuthPath}/me");
        Assert.Equal(HttpStatusCode.Unauthorized, staleSessionResponse.StatusCode);
    }

    [Fact]
    public async Task Logs_de_falha_de_login_nao_contem_dados_de_autenticacao()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(HttpsClientOptions());
        var token = await GetAntiforgeryTokenAsync(client);

        using var response = await PostWithTokenAsync(
            client,
            $"{AuthPath}/login",
            token,
            new { Email = "missing@whatsenvio.test", Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var logs = string.Join(Environment.NewLine, factory.Logs);
        Assert.DoesNotMatch("(?i)password|token|bearer|hash", logs);
    }

    private static WebApplicationFactoryClientOptions HttpsClientOptions()
        => new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true };

    private static async Task CreateUserAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var result = await users.CreateAsync(
            new AppUser { Email = Email, UserName = Email, LockoutEnabled = true },
            Password);

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Code)));
    }

    private static async Task LoginAsync(HttpClient client)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostWithTokenAsync(
            client,
            $"{AuthPath}/login",
            token,
            new { Email, Password });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync($"{AuthPath}/csrf");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var cookie = Assert.Single(
            SetCookies(response),
            value => value.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        return Uri.UnescapeDataString(cookie["XSRF-TOKEN=".Length..].Split(';', 2)[0]);
    }

    private static async Task<HttpResponseMessage> PostWithTokenAsync(
        HttpClient client,
        string path,
        string token,
        object? body = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-XSRF-TOKEN", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private static string[] SetCookies(HttpResponseMessage response)
        => response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.ToArray()
            : [];

    private sealed record UserDetailsResponse(string Id, string Name);

    private sealed class CapturingIdentityEmailSender : IIdentityEmailSenderService
    {
        public string? Token { get; private set; }

        public Task PasswordResetEmailSend(AppUser user, string token)
        {
            Token = token;
            return Task.CompletedTask;
        }
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly CapturingLoggerProvider _loggerProvider = new();
        private readonly string _databaseName = $"identity-tests-{Guid.NewGuid():N}";

        public IReadOnlyCollection<string> Logs => _loggerProvider.Messages;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:WhatsEnvio",
                "Host=localhost;Database=whatsenvio_tests;Username=test;Password=test");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDataProtectionProvider>();
                services.AddSingleton<IDataProtectionProvider>(_ => new EphemeralDataProtectionProvider());
                services.RemoveAll<IIdentityEmailSenderService>();
                services.AddSingleton<CapturingIdentityEmailSender>();
                services.AddSingleton<IIdentityEmailSenderService>(provider =>
                    provider.GetRequiredService<CapturingIdentityEmailSender>());
                services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
                services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                services.RemoveAll<IdentityDbContext>();
                services.AddDbContext<IdentityDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
                services.Configure<SecurityStampValidatorOptions>(options =>
                    options.ValidationInterval = TimeSpan.Zero);
                services.AddLogging(logging => logging
                    .AddProvider(_loggerProvider)
                    .AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.None));
            });
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _messages = new();

        public IReadOnlyCollection<string> Messages => _messages.ToArray();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _messages);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(string categoryName, ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                messages.Enqueue($"{categoryName}: {formatter(state, exception)} {exception}");
            }
        }
    }
}

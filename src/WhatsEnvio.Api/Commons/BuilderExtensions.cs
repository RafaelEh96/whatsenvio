using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry.Trace;
using WhatsEnvio.Api.Authorization;
using WhatsEnvio.Core.Enums;
using WhatsEnvio.Core.Observability;
using WhatsEnvio.Core.Tenancy;
using WhatsEnvio.Modules.Identity.Application.Interfaces;
using WhatsEnvio.Modules.Identity.Infrastructure;
using WhatsEnvio.Modules.Identity.Persistence;
using WhatsEnvio.Modules.Identity.Persistence.Context;
using WhatsEnvio.Modules.Identity.Persistence.Users;
using WhatsEnvio.Modules.Tenancy.Persistence;

namespace WhatsEnvio.Api.Commons;

public static class BuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public void AddConfigurations()
        {
            Configurations.ConnectionString = builder.Configuration.GetConnectionString("WhatsEnvio") ?? string.Empty;
        }

        public void AddDbContext()
        {
            builder.Services.AddTenancyModule(Configurations.ConnectionString);
            builder.Services.AddIdentityModule(Configurations.ConnectionString);
        }

        public void AddDocumentation()
        {
            builder.Services.AddOpenApi();
        }

        public void AddServices()
        {
            builder.Services.AddTransient<IIdentityEmailSenderService, IdentityEmailSenderService>();
            builder.Services.AddSingleton<TenantContextAccessor>();
            builder.Services.AddSingleton<ITenantContext>(
                sp => sp.GetRequiredService<TenantContextAccessor>());
            builder.Services.AddSingleton<ITenantContextAccessor>(
                sp => sp.GetRequiredService<TenantContextAccessor>());
        }

        public void AddTelemetry()
        {
            builder.Services.AddWhatsEnvioTelemetry(
                serviceName: "whatsenvio-api",
                useConsoleExporter: builder.Environment.IsDevelopment());

            builder.Services.AddOpenTelemetry()
                .WithTracing(t => t.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());
        }

        public void AddIdentityAuthentication()
        {
            builder.Services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            builder.AddCookies();
            builder.AddAuthorization();
        }

        private void AddCookies()
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();
        }

        public void AddCookiesConfigurations()
        {
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "WhatsEnvio.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

                options.Events.OnRedirectToLogin = context =>
                    Results.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "Autenticação necessária"
                        )
                    .ExecuteAsync(context.HttpContext);

                options.Events.OnRedirectToAccessDenied = context =>
                    Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso negado"
                        )
                    .ExecuteAsync(context.HttpContext);
            });
        }

        public void AddAntiforgeryConfiguration()
        {
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-XSRF-TOKEN";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
        }

        public void AddCookieRevogeConfiguration()
        {
            builder.Services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromMinutes(1);
            });
        }

        private void AddAuthorization()
        {
            builder.Services.AddScoped<IAuthorizationHandler, TenantAuthorizationHandler>();

            builder.Services.AddAuthorization(options =>
            {
                AddMemberPolicy(TenantPolicies.Member, options);
                AddAdminPolicy(TenantPolicies.Admin, options);
                AddOwnerPolicy(TenantPolicies.Owner, options);
            });
        }


    }

    private static void AddMemberPolicy(
        string policyName,
        AuthorizationOptions options)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new TenantRequirement(
                MembershipRole.Professional,
                MembershipRole.Admin,
                MembershipRole.Owner));
        });
    }

    private static void AddAdminPolicy(
        string policyName,
        AuthorizationOptions options)
    {
        options.AddPolicy(policyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new TenantRequirement(
                    MembershipRole.Admin,
                    MembershipRole.Owner));
            }
        );
    }

    private static void AddOwnerPolicy(
        string policyName,
        AuthorizationOptions options)
    {
        options.AddPolicy(policyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new TenantRequirement(
                    MembershipRole.Owner));
            }
        );
    }
}

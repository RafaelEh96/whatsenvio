
using WhatsEnvio.Worker.Commons;
using WhatsEnvio.Core.Tenancy;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.AddTelemetry();
builder.Services.AddSingleton<TenantContextAccessor>();
builder.Services.AddSingleton<ITenantContext>(sp => sp.GetRequiredService<TenantContextAccessor>());
builder.Services.AddSingleton<ITenantContextAccessor>(sp => sp.GetRequiredService<TenantContextAccessor>());

var host = builder.Build();
host.Run();

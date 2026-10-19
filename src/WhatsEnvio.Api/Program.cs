using WhatsEnvio.Api.Commons;
using WhatsEnvio.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigurations();
builder.AddDbContext();
builder.AddIdentityAuthentication();
builder.AddCookiesConfigurations();
builder.AddAntiforgeryConfiguration();
builder.AddCookieRevogeConfiguration();
builder.AddServices();
builder.AddTelemetry();
builder.AddDocumentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapEndpoints();

app.Run();

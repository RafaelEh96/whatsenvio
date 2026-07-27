using WhatsEnvio.Api.Commons;
using WhatsEnvio.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigurations();
builder.AddDbContext();
builder.AddTelemetry();
builder.AddDocumentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapEndpoints();

app.Run();

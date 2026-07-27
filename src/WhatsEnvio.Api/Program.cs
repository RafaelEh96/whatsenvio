using WhatsEnvio.Api.Commons;
using WhatsEnvio.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigurations();
builder.AddDbContext();
builder.AddDocumentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapEndpoints();

app.Run();

using WhatsEnvio.Modules.Tenancy.Infrastructure;

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
        }

        public void AddDocumentation()
        {
            builder.Services.AddOpenApi();
        }

    }
}

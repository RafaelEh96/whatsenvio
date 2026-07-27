namespace WhatsEnvio.Api.Commons;

public interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}

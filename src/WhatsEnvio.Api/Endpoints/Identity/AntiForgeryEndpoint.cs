using Microsoft.AspNetCore.Antiforgery;
using WhatsEnvio.Api.Commons;

namespace WhatsEnvio.Api.Endpoints.Identity;

public class AntiForgeryEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/csrf", Handle);

    private static IResult Handle(IAntiforgery antiforgery, HttpContext context)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);

        context.Response.Cookies.Append(
                "XSRF-TOKEN",
                tokens.RequestToken!,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                }
            );

        return TypedResults.NoContent();
    }
}

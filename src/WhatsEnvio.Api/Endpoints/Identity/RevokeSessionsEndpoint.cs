using Microsoft.AspNetCore.Identity;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Api.Endpoints.Identity;

public class RevokeSessionsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/revoke-sessions", HandleAsync)
            .WithName("RevokeSessions")
            .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        var user = await userManager.GetUserAsync(context.User);
        if (user is not null)
        {
            var result = await userManager.UpdateSecurityStampAsync(user);

            if (!result.Succeeded)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Não foi possível revogar as sessões");
            }

            await signInManager.SignOutAsync();
            return TypedResults.NoContent();
        }
        else
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Usuário não encontrado");
        }
    }
}

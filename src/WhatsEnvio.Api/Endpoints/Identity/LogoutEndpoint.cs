using Microsoft.AspNetCore.Identity;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Modules.Identity.Audit;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Api.Endpoints.Identity;

public class LogoutEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization();

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IdentityAuditService auditService,
        CancellationToken token)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Usuário não encontrado");
        }
        await signInManager.SignOutAsync();

        await auditService.LogoutAsync(user.Id, token);

        return TypedResults.NoContent();
    }
}

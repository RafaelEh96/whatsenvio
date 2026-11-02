using Microsoft.AspNetCore.Identity;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Core.Dtos.Identity;
using WhatsEnvio.Modules.Identity.Audit;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Api.Endpoints.Identity;

public class LoginEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous();

    private static async Task<IResult> LoginAsync(
        LoginRequestDto request,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IdentityAuditService auditService,
        CancellationToken token)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is not null)
        {
            var result = await signInManager.PasswordSignInAsync(
                    user,
                    request.Password,
                    isPersistent: false,
                    lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await auditService.LoginSucceededAsync(user.Id, token);

                return TypedResults.NoContent();
            }
        }

        await auditService.LoginFailedAync(user?.Id, token);

        return TypedResults.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Credenciais Invalidas");
    }
}

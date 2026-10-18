using Microsoft.AspNetCore.Identity;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Core.Dtos.Identity;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Api.Endpoints.Identity;

public class UserDetailsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/me", HandleAsync)
            .WithName("UserDetails")
            .RequireAuthorization();

    private static async Task HandleAsync(UserManager<AppUser> userManager, HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is not null)
        {
            var userDetails = new UserDetailsDto(Id: user.Id.ToString(), Name: user.Email ?? "Unknown");
            await context.Response.WriteAsJsonAsync(userDetails);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}

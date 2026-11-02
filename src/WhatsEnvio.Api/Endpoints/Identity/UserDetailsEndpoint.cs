using Microsoft.AspNetCore.Identity;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Core.Dtos.Identity;
using WhatsEnvio.Modules.Identity.Persistence.Users;
using WhatsEnvio.Modules.Tenancy.Contracts;

namespace WhatsEnvio.Api.Endpoints.Identity;

public class UserDetailsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/me", HandleAsync)
            .WithName("UserDetails")
            .RequireAuthorization();

    private static async Task HandleAsync(
        UserManager<AppUser> userManager,
        HttpContext context,
        IMembershipService  membershipService,
        CancellationToken token)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is not null)
        {
            var tenants = await membershipService
                .ListActiveAsync(userId: user.Id, token: token);

            var userDetails = new UserDetailsDto(
                Id: user.Id.ToString(),
                Name: user.Email ?? "Unknown",
                Tenants: tenants);
            await context.Response.WriteAsJsonAsync(
                userDetails,
                cancellationToken: token);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
}

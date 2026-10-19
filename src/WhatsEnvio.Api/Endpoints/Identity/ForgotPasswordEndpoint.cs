using Microsoft.AspNetCore.Identity;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Core.Dtos.Identity;
using WhatsEnvio.Modules.Identity.Application.Interfaces;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Api.Endpoints.Identity;

public class ForgotPasswordEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/forgot-password", HandleAsync)
            .WithName("ForgotPassword")
            .WithSummary("Request a password reset link.")
            .AllowAnonymous();

    private static async Task<IResult> HandleAsync(
        ForgotPasswordRequestDto request,
        IIdentityEmailSenderService emailSender,
        UserManager<AppUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await emailSender.PasswordResetEmailSend(user, token);
        }
        return TypedResults.StatusCode(StatusCodes.Status202Accepted);
    }
}

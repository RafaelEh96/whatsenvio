using System.Text;
using Microsoft.AspNetCore.Identity;
using WhatsEnvio.Api.Commons;
using WhatsEnvio.Core.Dtos.Identity;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Api.Endpoints.Identity;

public class ResetPasswordEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/reset-password", HandleAsync)
            .WithName("ResetPassword")
            .AllowAnonymous();

    private static async Task<IResult> HandleAsync(
        ResetPasswordRequestDto request,
        UserManager<AppUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Token inválido ou expirado");
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Token inválido ou expirado");
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(Convert.FromBase64String(request.Token));
        }
        catch (FormatException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Token inválido ou expirado");
        }

        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == "InvalidToken"))
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Token inválido ou expirado");
            }

            var passwordErrors = result.Errors
                .Where(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
                .Select(error => error.Description)
                .ToArray();
            if (passwordErrors.Length > 0)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.NewPassword)] = passwordErrors
                });
            }

            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Não foi possível redefinir a senha");
        }

        return TypedResults.NoContent();
    }
}

namespace WhatsEnvio.Core.Dtos.Identity;

public record ResetPasswordRequestDto(string Email, string Token, string NewPassword);

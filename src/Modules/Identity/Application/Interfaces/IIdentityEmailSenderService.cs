using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Modules.Identity.Application.Interfaces;

public interface IIdentityEmailSenderService
{
    Task PasswordResetEmailSend(AppUser user, string token);
}

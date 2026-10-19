using System.Net.Mail;
using WhatsEnvio.Modules.Identity.Application.Interfaces;
using WhatsEnvio.Modules.Identity.Persistence.Users;

namespace WhatsEnvio.Modules.Identity.Infrastructure;

public class IdentityEmailSenderService : IIdentityEmailSenderService
{
    public async Task PasswordResetEmailSend(AppUser user, string token)
    {
        using var smtpClient = CreateSmtpClient();
        using var mailMessage = new MailMessage
        {
            From = new MailAddress(Configurations.BaseEmailAddress),
            Subject = "Reset de Senha",
            Body = $"Olá {user.UserName}, use este código para redefinir sua senha: {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token))}"
        };
        mailMessage.To.Add(user.Email ?? throw new InvalidOperationException("Email do usuário não informado"));

        await smtpClient.SendMailAsync(mailMessage);
    }

    private static SmtpClient CreateSmtpClient()
    {
        var smtpClient = new SmtpClient(Configurations.BaseEmailSmtpConfig)
        {
            Port = 587,
            Credentials = new System.Net.NetworkCredential(Configurations.BaseEmailUsername, Configurations.BaseEmailPassword),
            EnableSsl = true,
        };
        return smtpClient;
    }
}

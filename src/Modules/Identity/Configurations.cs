namespace WhatsEnvio.Modules.Identity;

public static class Configurations
{
    public static string BaseEmailSmtpConfig => Environment.GetEnvironmentVariable("WHATSENVIO_EMAIL_SMTP_CONFIG") ?? string.Empty;
    public static string BaseEmailAddress => Environment.GetEnvironmentVariable("WHATSENVIO_EMAIL_ADDRESS") ?? string.Empty;
    public static string BaseEmailPassword => Environment.GetEnvironmentVariable("WHATSENVIO_EMAIL_PASSWORD") ?? string.Empty;
    public static string BaseEmailUsername => Environment.GetEnvironmentVariable("WHATSENVIO_EMAIL_USERNAME") ?? string.Empty;
}

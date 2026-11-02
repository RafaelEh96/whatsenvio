namespace WhatsEnvio.Modules.Identity.Audit;

public class IdentityAuditEventTypes
{
    public const string LoginSucceeded = "identity.login.succeeded";
    public const string LoginFailed = "identity.login.failed";
    public const string Logout =  "identity.logout";
    public const string SessionsRevoked = "identity.sessions.revoked";
}

using WhatsEnvio.Core.Observability;
using WhatsEnvio.Modules.Identity.Persistence.Context;

namespace WhatsEnvio.Modules.Identity.Audit;

public class IdentityAuditService(IdentityDbContext context)
{
    public Task LoginSucceededAsync(Guid userId, CancellationToken token = default)
        => WriteAsync(IdentityAuditEventTypes.LoginSucceeded, userId, token);

    public Task LoginFailedAync(Guid? userId, CancellationToken token = default)
        => WriteAsync(IdentityAuditEventTypes.LoginFailed, userId, token);

    public Task LogoutAsync(Guid userId, CancellationToken token = default)
        => WriteAsync(IdentityAuditEventTypes.Logout, userId, token);

    public Task SessionsRevokedAsync(Guid userId, CancellationToken token = default)
        => WriteAsync(IdentityAuditEventTypes.SessionsRevoked, userId, token);

    private async Task WriteAsync(string eventType, Guid? userId, CancellationToken token)
    {
        var correlationId = CorrelationId.Current;
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new InvalidOperationException("Não há correlationId para o evento de auditoria");

        var auditEvent = new IdentityAuditEvent
        {
            Id = Guid.CreateVersion7(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            TenantId = null,
            UserId = userId,
            EventType = eventType,
            Data = "{}",
            CorrelationId = correlationId
        };

        context.AuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(token);
    }
}

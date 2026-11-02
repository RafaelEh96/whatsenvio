namespace WhatsEnvio.Modules.Tenancy.Audit;

public class TenancyAuditEvent
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = "{}";
    public string CorrelationId { get; set; } = string.Empty;
}

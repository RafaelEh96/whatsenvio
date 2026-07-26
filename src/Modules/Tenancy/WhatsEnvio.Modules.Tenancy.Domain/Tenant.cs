namespace WhatsEnvio.Modules.Tenancy.Domain;

public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Tenant(string name, string timeZoneId, DateTimeOffset createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        Name = name;
        TimeZoneId = timeZoneId;
        CreatedAtUtc = createdAtUtc;
    }

    private Tenant() { }
}

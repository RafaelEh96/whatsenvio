namespace WhatsEnvio.Modules.Tenancy.Tenants.Model;

public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    public Tenant(string name, string timeZoneId, DateTimeOffset createdAtUtc)
    {
        if(ValidateTimeZoneId(timeZoneId) == null)
            throw new ArgumentException("Fuso horário não pode ser nulo ou vazio.", nameof(timeZoneId));

        if(ValidateTimeZone(timeZoneId) == null)
            throw new ArgumentException($"Fuso horário '{timeZoneId}' não encontrado.", nameof(timeZoneId));

        Id = Guid.CreateVersion7();
        Name = ValidateName(name);
        TimeZoneId = timeZoneId;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    private Tenant() { }

    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("Tenant já está inativo.");
        IsActive = false;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome não pode ser nulo ou vazio.", nameof(name));
        return name.Trim();
    }

    private static TimeZoneInfo ValidateTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Fuso horário '{timeZoneId}' não encontrado.", nameof(timeZoneId));
        }
    }

    private static string ValidateTimeZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Fuso horário não pode ser nulo ou vazio.", nameof(timeZoneId));
        return timeZoneId.Trim();
    }
}

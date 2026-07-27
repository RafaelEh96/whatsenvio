namespace WhatsEnvio.Core.Observability;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    public const string TagName = "whatsenvio.correlation_id";

    private static readonly AsyncLocal<string?> _current = new();

    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}

using System.Diagnostics;
using WhatsEnvio.Core.Observability;

namespace WhatsEnvio.Api.Commons;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Sanitize(context.Request.Headers[CorrelationId.HeaderName].FirstOrDefault())
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.CreateVersion7().ToString();

        CorrelationId.Current = correlationId;
        Activity.Current?.SetTag(CorrelationId.TagName, correlationId);
        context.Response.Headers[CorrelationId.HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string? Sanitize(string? value)
        => !string.IsNullOrWhiteSpace(value)
        && value.Length <= 128
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
        ? value
        : null;
}

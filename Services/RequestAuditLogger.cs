using System.Diagnostics;

namespace UserManagementAPI.Services;

/// <summary>
/// Writes one structured completion event for each HTTP request.
/// </summary>
public sealed class RequestAuditLogger(ILogger<RequestAuditLogger> logger)
{
    public void LogCompletion(
        HttpContext context,
        int statusCode,
        long startTimestamp,
        LogLevel logLevel = LogLevel.Information)
    {
        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        logger.Log(
            logLevel,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:F2} ms with trace ID {TraceId}.",
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            statusCode,
            elapsedMilliseconds,
            context.TraceIdentifier);
    }
}

using System.Diagnostics;
using UserManagementAPI.Services;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Audits successfully completed requests without recording headers or bodies.
/// </summary>
public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    RequestAuditLogger auditLogger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        await next(context);

        auditLogger.LogCompletion(
            context,
            context.Response.StatusCode,
            startTimestamp);
    }
}

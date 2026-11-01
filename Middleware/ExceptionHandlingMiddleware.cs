using System.Diagnostics;
using UserManagementAPI.Services;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Converts request and application exceptions into safe Problem Details responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    RequestAuditLogger auditLogger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        catch (BadHttpRequestException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogWarning(
                exception,
                "The request with trace ID {TraceId} could not be processed.",
                context.TraceIdentifier);

            await ProblemDetailsResponse.WriteAsync(
                context,
                exception.StatusCode,
                "Invalid request",
                "The request could not be read. Ensure the body contains valid JSON matching the expected schema.");

            auditLogger.LogCompletion(
                context,
                exception.StatusCode,
                startTimestamp,
                LogLevel.Warning);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                "The request with trace ID {TraceId} was cancelled by the client.",
                context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(
                    exception,
                    "An unhandled exception occurred after the response started for trace ID {TraceId}.",
                    context.TraceIdentifier);
                throw;
            }

            logger.LogError(
                exception,
                "An unhandled exception occurred for trace ID {TraceId}.",
                context.TraceIdentifier);

            await ProblemDetailsResponse.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "The server encountered an unexpected error while processing the request.");

            auditLogger.LogCompletion(
                context,
                StatusCodes.Status500InternalServerError,
                startTimestamp,
                LogLevel.Error);
        }
    }
}

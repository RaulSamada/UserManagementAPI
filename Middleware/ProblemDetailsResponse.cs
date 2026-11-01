using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Middleware;

internal static class ProblemDetailsResponse
{
    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        Action<IHeaderDictionary>? configureHeaders = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        configureHeaders?.Invoke(context.Response.Headers);

        return context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }
}

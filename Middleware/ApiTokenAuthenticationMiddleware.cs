using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using UserManagementAPI.Configuration;
using UserManagementAPI.Services;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Protects API routes with a configured bearer token.
/// </summary>
public sealed class ApiTokenAuthenticationMiddleware(
    RequestDelegate next,
    IOptions<ApiAuthenticationOptions> options,
    RequestAuditLogger auditLogger)
{
    private const string BearerPrefix = "Bearer ";
    private readonly string _expectedToken = options.Value.Token;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var startTimestamp = Stopwatch.GetTimestamp();

        if (HasValidToken(context))
        {
            await next(context);
            return;
        }

        await ProblemDetailsResponse.WriteAsync(
            context,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "A valid bearer token is required to access this endpoint.",
            headers => headers.WWWAuthenticate = "Bearer");

        auditLogger.LogCompletion(
            context,
            StatusCodes.Status401Unauthorized,
            startTimestamp,
            LogLevel.Warning);
    }

    private bool HasValidToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var providedToken = authorization[BearerPrefix.Length..].Trim();
        if (providedToken.Length == 0)
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(_expectedToken);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}

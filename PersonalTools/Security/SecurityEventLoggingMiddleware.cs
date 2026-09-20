using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace PersonalTools.Security;

/// <summary>
/// Records actionable security signals without capturing cookies, query strings or request bodies.
/// Events are briefly deduplicated per source and path so automated scans cannot flood the log.
/// </summary>
public sealed class SecurityEventLoggingMiddleware(RequestDelegate next, ILogger<SecurityEventLoggingMiddleware> logger, IMemoryCache cache)
{
    private static readonly string[] SuspiciousPathMarkers =
    [
        "/.env", "/.git", "/wp-admin", "/wp-login", "/phpmyadmin", "/adminer",
        "/config.php", "/server-status", "/actuator", "/cgi-bin", "/vendor/phpunit"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        int status = context.Response.StatusCode;
        string path = SafeValue(context.Request.Path.Value, 300);
        bool suspiciousProbe = status == StatusCodes.Status404NotFound &&
            SuspiciousPathMarkers.Any(marker => path.Contains(marker, StringComparison.OrdinalIgnoreCase));
        EventId? eventId = status switch
        {
            StatusCodes.Status401Unauthorized when !path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase) => SecurityEventIds.UnauthorizedRequest,
            StatusCodes.Status403Forbidden => SecurityEventIds.ForbiddenRequest,
            StatusCodes.Status405MethodNotAllowed => SecurityEventIds.UnsupportedHttpMethod,
            _ when suspiciousProbe => SecurityEventIds.SuspiciousPathProbe,
            _ => null
        };
        if (eventId is null) return;

        string source = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!ShouldLog(cache, eventId.Value, source, path)) return;

        logger.LogWarning(
            eventId.Value,
            "Security request rejected. Status {StatusCode}; method {Method}; path {Path}; source {RemoteIp}; user {UserId}; name {UserName}; agent {UserAgent}.",
            status,
            SafeValue(context.Request.Method, 16),
            path,
            source,
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
            SafeValue(context.User.Identity?.Name, 100, "anonymous"),
            SafeValue(context.Request.Headers.UserAgent.ToString(), 240));
    }

    internal static string SafeValue(string? value, int maximumLength, string fallback = "")
    {
        string safe = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        if (string.IsNullOrWhiteSpace(safe)) safe = fallback;
        return safe.Length <= maximumLength ? safe : safe[..maximumLength];
    }

    internal static bool ShouldLog(IMemoryCache cache, EventId eventId, string source, string path)
    {
        string throttleKey = $"security:{eventId.Id}:{source}:{path}";
        if (cache.TryGetValue(throttleKey, out _)) return false;
        cache.Set(throttleKey, true, TimeSpan.FromMinutes(5));
        return true;
    }
}

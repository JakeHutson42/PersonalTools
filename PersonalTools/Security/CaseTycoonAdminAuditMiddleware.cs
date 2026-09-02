using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using MySqlConnector;
using PersonalTools.Data;

namespace PersonalTools.Security;

public sealed class CaseTycoonAdminAuditMiddleware(RequestDelegate next, ILogger<CaseTycoonAdminAuditMiddleware> logger)
{
    private const int MaximumBodyCharacters = 65_536;

    public async Task InvokeAsync(HttpContext context, IMariaDbDataAccess database)
    {
        if (!ShouldAudit(context))
        {
            await next(context);
            return;
        }

        string requestJson = await ReadRequestBody(context.Request);
        DateTime startedUtc = DateTime.UtcNow;
        await next(context);
        if (context.Response.StatusCode >= 400) return;

        try
        {
            string actorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            string actorName = context.User.Identity?.Name ?? "Unknown administrator";
            string routeValues = string.Join(",", context.Request.RouteValues.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
            await database.ExecuteSP(
                "sp_case_tycoon_admin_audit_append",
                [
                    new MySqlParameter("p_audit_id", Guid.NewGuid().ToString("D")),
                    new MySqlParameter("p_actor_user_id", actorId),
                    new MySqlParameter("p_actor_display_name", actorName),
                    new MySqlParameter("p_http_method", context.Request.Method),
                    new MySqlParameter("p_request_path", context.Request.Path.Value ?? string.Empty),
                    new MySqlParameter("p_route_values", routeValues),
                    new MySqlParameter("p_submitted_json", requestJson),
                    new MySqlParameter("p_response_status", context.Response.StatusCode),
                    new MySqlParameter("p_remote_ip", context.Connection.RemoteIpAddress?.ToString() ?? string.Empty),
                    new MySqlParameter("p_user_agent", context.Request.Headers.UserAgent.ToString()),
                    new MySqlParameter("p_created_utc", startedUtc),
                ],
                CancellationToken.None);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            // Auditing must never turn a successful, already-applied admin change into a client error.
            logger.LogError(exception, "Failed to persist the Case Tycoon admin audit entry for {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true || !context.User.IsInRole(AppRole.Admin.ToString())) return false;
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method)) return false;
        PathString path = context.Request.Path;
        if (!path.StartsWithSegments("/api/case-opening") && !path.StartsWithSegments("/api/admin/case-battles")) return false;
        return context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(data => string.Equals(data.Policy, AppAuthorizationPolicies.AdminOnly, StringComparison.Ordinal)) == true;
    }

    private static async Task<string> ReadRequestBody(HttpRequest request)
    {
        if (request.ContentLength == 0) return string.Empty;
        request.EnableBuffering();
        using StreamReader reader = new(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        char[] buffer = new char[MaximumBodyCharacters + 1];
        int read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
        request.Body.Position = 0;
        string value = new(buffer, 0, Math.Min(read, MaximumBodyCharacters));
        return read > MaximumBodyCharacters ? value + "…[truncated]" : value;
    }
}

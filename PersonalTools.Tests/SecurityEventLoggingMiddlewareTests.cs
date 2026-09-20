using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PersonalTools.Security;

namespace PersonalTools.Tests;

public sealed class SecurityEventLoggingMiddlewareTests
{
    [Fact]
    public async Task RejectedRequest_LogsKnownUserWithoutQueryOrSensitiveHeaders()
    {
        CapturingLogger logger = new();
        using MemoryCache cache = new(new MemoryCacheOptions());
        SecurityEventLoggingMiddleware middleware = new(
            context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; },
            logger,
            cache);
        DefaultHttpContext context = new();
        context.Request.Path = "/admin/private";
        context.Request.QueryString = new QueryString("?token=must-not-appear");
        context.Request.Headers.Cookie = "PersonalTools.Auth=must-not-appear";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "4cf1fa87-d0fb-49b1-8c40-3e5d7a61c101"),
            new Claim(ClaimTypes.Name, "TEST")
        ], "test"));

        await middleware.InvokeAsync(context);

        Assert.Equal(SecurityEventIds.ForbiddenRequest.Id, logger.EventId.Id);
        Assert.Contains("TEST", logger.Message, StringComparison.Ordinal);
        Assert.Contains("4cf1fa87-d0fb-49b1-8c40-3e5d7a61c101", logger.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("must-not-appear", logger.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RepeatedProbe_IsDeduplicatedForFiveMinuteWindow()
    {
        CapturingLogger logger = new();
        using MemoryCache cache = new(new MemoryCacheOptions());
        SecurityEventLoggingMiddleware middleware = new(
            context => { context.Response.StatusCode = StatusCodes.Status404NotFound; return Task.CompletedTask; },
            logger,
            cache);
        DefaultHttpContext context = new();
        context.Request.Path = "/.env";

        await middleware.InvokeAsync(context);
        await middleware.InvokeAsync(context);

        Assert.Equal(1, logger.WriteCount);
        Assert.Equal(SecurityEventIds.SuspiciousPathProbe.Id, logger.EventId.Id);
    }

    private sealed class CapturingLogger : ILogger<SecurityEventLoggingMiddleware>
    {
        public EventId EventId { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public int WriteCount { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            EventId = eventId;
            Message = formatter(state, exception);
            WriteCount++;
        }
    }
}

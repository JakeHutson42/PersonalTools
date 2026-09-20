using Microsoft.Extensions.Logging;

namespace PersonalTools.Security;

/// <summary>Stable identifiers make security events easy to filter in the existing log viewer.</summary>
public static class SecurityEventIds
{
    public static readonly EventId LoginRejected = new(4001, nameof(LoginRejected));
    public static readonly EventId RateLimitRejected = new(4002, nameof(RateLimitRejected));
    public static readonly EventId InvalidAuthenticationTicket = new(4003, nameof(InvalidAuthenticationTicket));
    public static readonly EventId GuestBoundaryRejected = new(4004, nameof(GuestBoundaryRejected));
    public static readonly EventId UnauthorizedRequest = new(4010, nameof(UnauthorizedRequest));
    public static readonly EventId ForbiddenRequest = new(4011, nameof(ForbiddenRequest));
    public static readonly EventId SuspiciousPathProbe = new(4012, nameof(SuspiciousPathProbe));
    public static readonly EventId UnsupportedHttpMethod = new(4013, nameof(UnsupportedHttpMethod));
}

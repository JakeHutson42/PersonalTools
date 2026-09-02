using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PersonalTools.Hubs;

[Authorize(Policy = PersonalTools.Security.AppAuthorizationPolicies.CaseTycoonAccess)]
public sealed class LiveWinnersHub : Hub;

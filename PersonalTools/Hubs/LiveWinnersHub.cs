using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PersonalTools.Hubs;

[Authorize]
public sealed class LiveWinnersHub : Hub;

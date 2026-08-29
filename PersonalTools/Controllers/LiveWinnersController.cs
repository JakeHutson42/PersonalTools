using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalTools.Classes;
using PersonalTools.Entities;

namespace PersonalTools.Controllers;

[Authorize]
[ApiController]
[Route("api/live-winners")]
public sealed class LiveWinnersController : ControllerBase
{
    private readonly ILiveWinnersFuncs _winners;
    public LiveWinnersController(ILiveWinnersFuncs winners) => _winners = winners;
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public Task<LiveWinnersSummaryObj> Get(CancellationToken cancellationToken) => _winners.Get(UserId, cancellationToken);
}

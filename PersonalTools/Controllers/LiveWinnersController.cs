using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalTools.Classes;
using PersonalTools.Entities;
using PersonalTools.Security;

namespace PersonalTools.Controllers;

[Authorize]
[ApiController]
[Route("api/live-winners")]
public sealed class LiveWinnersController : ControllerBase
{
    private readonly ILiveWinnersFuncs _winners;
    public LiveWinnersController(ILiveWinnersFuncs winners) => _winners = winners;
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public Task<LiveWinnersSummaryObj> Get(CancellationToken cancellationToken) => _winners.Get(UserId, User.IsUserAllowedHere(AppRole.Admin), cancellationToken);
    [HttpPut("visibility")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> SetVisibility([FromBody] LiveWinnersVisibilityRequest request, CancellationToken cancellationToken)
    {
        try { await _winners.SetVisibility(request.Visibility, cancellationToken); return Ok(new ApiResponse(true, "Winners dock visibility saved.")); }
        catch (InvalidOperationException exception) { return BadRequest(new ApiResponse(false, exception.Message)); }
    }
}
public sealed record LiveWinnersVisibilityRequest(string Visibility);

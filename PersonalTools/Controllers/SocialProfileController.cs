using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalTools.Classes;
using PersonalTools.Entities;

namespace PersonalTools.Controllers;

[Authorize, ApiController, Route("api/social")]
public sealed class SocialProfileController(ISocialProfileFuncs social, ILogger<SocialProfileController> logger) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("summary")] public Task<ActionResult> Summary(CancellationToken ct) => Run(() => social.GetSummary(UserId, ct));
    [HttpGet("search")] public Task<ActionResult> Search([FromQuery] string query, CancellationToken ct) => Run(() => social.Search(UserId, query ?? string.Empty, ct));
    [HttpPost("friends/{friendUserId:guid}")] public Task<ActionResult> Add(Guid friendUserId, CancellationToken ct) => Run(async () => { await social.AddFriend(UserId, friendUserId, ct); return new ApiResponse(true, "Friend added."); });
    [HttpDelete("friends/{friendUserId:guid}")] public Task<ActionResult> Remove(Guid friendUserId, CancellationToken ct) => Run(async () => { await social.RemoveFriend(UserId, friendUserId, ct); return new ApiResponse(true, "Friend removed."); });
    private async Task<ActionResult> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (InvalidOperationException exception) { return BadRequest(new ApiResponse(false, exception.Message)); }
        catch (Exception exception) { logger.LogError(exception, "Social profile request failed for {UserId}.", UserId); return StatusCode(500, new ApiResponse(false, "The social profile could not be updated.")); }
    }
}

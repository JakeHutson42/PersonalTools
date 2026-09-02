using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalTools.Security;
using Microsoft.AspNetCore.RateLimiting;
using MySqlConnector;
using PersonalTools.Classes;
using PersonalTools.Classes.CaseBattles;
using PersonalTools.Entities;
using PersonalTools.Entities.CaseBattles;

namespace PersonalTools.Controllers;

[Authorize(Policy = AppAuthorizationPolicies.CaseTycoonAccess)]
[ApiController]
[Route("api/case-battles")]
public sealed class CaseBattlesController(ICaseBattleFuncs battles, ILogger<CaseBattlesController> logger) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("active")] public Task<ActionResult<CaseBattleSummaryObj?>> GetActive(CancellationToken ct) => Execute(() => battles.GetActive(UserId, ct));
    [HttpGet("history")] public Task<ActionResult<List<CaseBattleHistoryObj>>> GetHistory(CancellationToken ct) => Execute(() => battles.GetHistory(UserId, ct));
    [HttpGet("invitable-users")] public Task<ActionResult<List<CaseBattleInvitableUserObj>>> GetInvitableUsers(CancellationToken ct) => Execute(() => battles.GetInvitableUsers(UserId, ct));
    [HttpGet("invitable-users/{invitedUserId:guid}/unlocked-cases")] public Task<ActionResult<List<string>>> GetInvitableUserUnlockedCases(Guid invitedUserId, CancellationToken ct) => Execute(() => battles.GetInvitableUserUnlockedCases(UserId, invitedUserId, ct));
    [HttpGet("bot-status")] public Task<ActionResult<CaseBattleBotStatusObj>> GetBotStatus(CancellationToken ct) => Execute(() => battles.GetBotStatus(ct));
    [HttpGet("timings")] public Task<ActionResult<CaseBattleTimingSettingsObj>> GetTimings(CancellationToken ct) => Execute(() => battles.GetTimingSettings(ct));
    [HttpGet("invitations/pending")] public Task<ActionResult<List<CaseBattleInvitationObj>>> GetPendingInvitations(CancellationToken ct) => Execute(() => battles.GetPendingInvitations(UserId, ct));
    [HttpGet("invitations/created")] public Task<ActionResult<List<CaseBattlePendingCreatedObj>>> GetPendingCreated(CancellationToken ct) => Execute(() => battles.GetPendingCreated(UserId, ct));
    [HttpGet("{battleId:guid}")] public Task<ActionResult<CaseBattleSummaryObj>> Get(Guid battleId, CancellationToken ct) => Execute(() => battles.Get(UserId, battleId, ct));
    [HttpGet("{battleId:guid}/detail")] public Task<ActionResult<CaseBattleDetailObj>> GetDetail(Guid battleId, CancellationToken ct) => Execute(() => battles.GetDetail(UserId, battleId, ct));
    [HttpPost, EnableRateLimiting("case-battles-write")] public Task<ActionResult<CaseBattleSummaryObj>> Create([FromBody] CaseBattleCreateRequestObj request, CancellationToken ct) => Execute(() => battles.Create(UserId, request ?? new(), ct));
    [HttpPost("buy-all"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<CaseBattleBuyAllResultObj>> BuyAll([FromBody] CaseBattleBuyAllRequestObj request, CancellationToken ct) => Execute(() => battles.BuyAll(UserId, request ?? new(), ct));
    [HttpPost("{battleId:guid}/join"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<CaseBattleSummaryObj>> Join(Guid battleId, CancellationToken ct) => Execute(() => battles.Join(UserId, battleId, ct));
    [HttpPost("{battleId:guid}/invitations"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<CaseBattleDetailObj>> InviteParticipants(Guid battleId, [FromBody] CaseBattleInviteRequestObj request, CancellationToken ct) => Execute(() => battles.InviteParticipants(UserId, battleId, request ?? new(), ct));
    [HttpPost("{battleId:guid}/invite/accept"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<CaseBattleSummaryObj>> AcceptInvite(Guid battleId, CancellationToken ct) => Execute(() => battles.AcceptInvite(UserId, battleId, ct));
    [HttpPost("{battleId:guid}/invite/buy-and-accept"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<CaseBattleSummaryObj>> BuyAndAcceptInvite(Guid battleId, CancellationToken ct) => Execute(() => battles.BuyMissingCasesAndAcceptInvite(UserId, battleId, ct));
    [HttpPost("{battleId:guid}/invite/decline"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<ApiResponse>> DeclineInvite(Guid battleId, CancellationToken ct) => Execute(async () => { await battles.DeclineInvite(UserId, battleId, ct); return new ApiResponse(true, "Invitation declined."); });
    [HttpPut("{battleId:guid}/ready"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<CaseBattleSummaryObj>> Ready(Guid battleId, [FromBody] bool isReady, CancellationToken ct) => Execute(() => battles.SetReady(UserId, battleId, isReady, ct));
    [HttpPost("{battleId:guid}/start"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<CaseBattleSummaryObj>> Start(Guid battleId, CancellationToken ct) => Execute(() => battles.Start(UserId, battleId, ct));
    [HttpPost("{battleId:guid}/cancel"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<ApiResponse>> Cancel(Guid battleId, CancellationToken ct) => Execute(async () => { await battles.Cancel(UserId, battleId, ct); return new ApiResponse(true, "Battle cancelled and cases returned."); });
    [HttpPost("{battleId:guid}/leave"), EnableRateLimiting("case-battles-write")] public Task<ActionResult<ApiResponse>> Leave(Guid battleId, CancellationToken ct) => Execute(async () => { await battles.Leave(UserId, battleId, ct); return new ApiResponse(true, "You left the battle and your cases were returned."); });
    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> operation)
    {
        try { return Ok(await operation()); }
        // Page navigation and invitation polling can cancel a request while a pooled connection is opening.
        // The work is abandoned before a procedure runs, so this is not an application or battle failure.
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { return StatusCode(499); }
        catch (KeyNotFoundException) { return NotFound(new ApiResponse(false, "This battle is unavailable.")); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiResponse(false, ex.Message)); }
        // Stored procedures deliberately use SQLSTATE 45000 for player-facing validation
        // (balance, inventory capacity, and similar checks).  Keep infrastructure faults private,
        // but return those safe messages instead of disguising them as a server error.
        catch (MySqlException ex) when (string.Equals(ex.SqlState, "45000", StringComparison.Ordinal)) { return BadRequest(new ApiResponse(false, ex.Message)); }
        catch (Exception ex) { logger.LogError(ex, "Case battle request failed for {UserId}", UserId); return StatusCode(500, new ApiResponse(false, "The case battle could not be updated.")); }
    }
}

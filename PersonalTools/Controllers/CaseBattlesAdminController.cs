using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalTools.Classes;
using PersonalTools.Classes.CaseBattles;
using PersonalTools.Entities;
using PersonalTools.Entities.CaseBattles;
using PersonalTools.Security;

namespace PersonalTools.Controllers;

[Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
[ApiController]
[Route("api/admin/case-battles")]
public sealed class CaseBattlesAdminController(ICaseBattleFuncs battles, ILogger<CaseBattlesAdminController> logger) : ControllerBase
{
    [HttpGet("bot-status")] public async Task<ActionResult<CaseBattleBotStatusObj>> GetBotStatus(CancellationToken cancellationToken) => Ok(await battles.GetBotStatus(cancellationToken));
    [HttpGet("timings")] public async Task<ActionResult<CaseBattleTimingSettingsObj>> GetTimings(CancellationToken cancellationToken) => Ok(await battles.GetTimingSettings(cancellationToken));
    [HttpPut("timings")] public async Task<ActionResult<CaseBattleTimingSettingsObj>> SetTimings([FromBody] CaseBattleTimingSettingsObj settings, CancellationToken cancellationToken) { await battles.SetTimingSettings(settings ?? new(), cancellationToken); return Ok(await battles.GetTimingSettings(cancellationToken)); }
    [HttpPut("feature-status")] public async Task<ActionResult<CaseBattleBotStatusObj>> SetFeatureStatus([FromBody] bool enabled, CancellationToken cancellationToken) { await battles.SetFeatureEnabled(enabled, cancellationToken); return Ok(await battles.GetBotStatus(cancellationToken)); }
    [HttpPut("ffa-3-status")] public async Task<ActionResult<CaseBattleBotStatusObj>> SetFreeForAll3Status([FromBody] bool enabled, CancellationToken cancellationToken) { await battles.SetFreeForAll3Enabled(enabled, cancellationToken); return Ok(await battles.GetBotStatus(cancellationToken)); }
    [HttpPut("bot-status")] public async Task<ActionResult<CaseBattleBotStatusObj>> SetBotStatus([FromBody] bool enabled, CancellationToken cancellationToken) { await battles.SetBotEnabled(enabled, cancellationToken); return Ok(await battles.GetBotStatus(cancellationToken)); }
    [HttpGet]
    public async Task<ActionResult<List<CaseBattleAdminReconciliationObj>>> Get(CancellationToken cancellationToken)
    {
        try { return Ok(await battles.GetAdminReconciliation(cancellationToken)); }
        catch (Exception exception) { logger.LogError(exception, "Could not load case battle reconciliation data."); return StatusCode(500, new ApiResponse(false, "Case battle reconciliation data could not be loaded.")); }
    }

    [HttpPost("{battleId:guid}/expire")]
    public async Task<ActionResult<ApiResponse>> Expire(Guid battleId, CancellationToken cancellationToken)
    {
        try { await battles.ReconcileExpired(battleId, cancellationToken); return Ok(new ApiResponse(true, "Expired waiting battle reconciled.")); }
        catch (Exception exception) { logger.LogError(exception, "Could not reconcile expired case battle {BattleId}.", battleId); return StatusCode(500, new ApiResponse(false, "The expired battle could not be reconciled.")); }
    }

    [HttpPost("{battleId:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid battleId, CancellationToken cancellationToken)
    {
        try { await battles.CancelPendingAsAdmin(battleId, cancellationToken); return Ok(new ApiResponse(true, "Pending battle cancelled and escrow returned.")); }
        catch (MySqlConnector.MySqlException exception) when (exception.SqlState == "45000") { return BadRequest(new ApiResponse(false, exception.Message)); }
        catch (Exception exception) { logger.LogError(exception, "Could not cancel pending case battle {BattleId}.", battleId); return StatusCode(500, new ApiResponse(false, "The pending battle could not be cancelled.")); }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalTools.Classes;
using PersonalTools.Classes.CaseOpening;
using PersonalTools.Entities;
using PersonalTools.Entities.CaseOpening;
using PersonalTools.Security;

namespace PersonalTools.Controllers;

[Authorize]
[ApiController]
[Route("api/case-opening")]
public sealed class CaseOpeningController : ControllerBase
{
    private readonly ICaseOpeningFuncs _caseOpening;
    private readonly IAuthFuncs _auth;
    private readonly ILogger<CaseOpeningController> _logger;

    public CaseOpeningController(ICaseOpeningFuncs caseOpening, IAuthFuncs auth, ILogger<CaseOpeningController> logger)
    {
        _caseOpening = caseOpening;
        _auth = auth;
        _logger = logger;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("cases")]
    public Task<ActionResult<List<CaseOpeningCaseSummaryObj>>> GetCases(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningCases(UserId, cancellationToken), "load catalogue", "curated");
    }

    [HttpGet("cases/{caseKey}")]
    public Task<ActionResult<CaseOpeningCaseObj>> GetCase(string caseKey, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningCase(UserId, caseKey, cancellationToken), "load", caseKey);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<CaseOpeningHistoryObj>>> GetHistory(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _caseOpening.GetCaseOpeningHistory(UserId, cancellationToken));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Case-opening history failed for user {UserId}.", UserId);
            return StatusCode(500, new ApiResponse(false, "Your case-opening history could not be loaded."));
        }
    }

    [HttpGet("cases/{caseKey}/collection")]
    public Task<ActionResult<CaseOpeningCollectionObj>> GetCollection(string caseKey, CancellationToken cancellationToken)
    {
        return Execute(
            () => _caseOpening.GetCaseOpeningCollection(UserId, caseKey, cancellationToken),
            "load collection",
            caseKey);
    }

    [HttpGet("collections")]
    public Task<ActionResult<List<CaseOpeningCollectionSummaryObj>>> GetCollections(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningCollections(UserId, cancellationToken), "load collections", "all");
    }

    [HttpGet("progress")]
    public Task<ActionResult<CaseOpeningProgressObj>> GetProgress(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningProgress(UserId, cancellationToken), "load progress", "all");
    }

    [HttpGet("inventory-capacity")]
    public Task<ActionResult<CaseOpeningInventoryCapacityObj>> GetInventoryCapacity(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningInventoryCapacity(UserId, cancellationToken), "load inventory capacity", "all");
    }

    [HttpGet("player-stats")]
    public Task<ActionResult<CaseOpeningPlayerStatsObj>> GetPlayerStats(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningPlayerStats(UserId, cancellationToken), "load player stats", "all");
    }

    [HttpGet("achievements")]
    public Task<ActionResult<CaseOpeningAchievementSummaryObj>> GetAchievements(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningAchievements(UserId, cancellationToken), "load achievements", "all");
    }

    [HttpPost("upgrades/{upgradeKey}/unlock")]
    public Task<ActionResult<CaseOpeningProgressObj>> UnlockUpgrade(string upgradeKey, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.UnlockCaseOpeningUpgrade(UserId, upgradeKey, cancellationToken), "unlock upgrade", upgradeKey);
    }

    [HttpPost("cases/{caseKey}/unlock")]
    public Task<ActionResult<CaseOpeningProgressObj>> UnlockCase(string caseKey, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.UnlockCaseOpeningCase(UserId, caseKey, cancellationToken), "unlock case", caseKey);
    }

    [HttpPost("inventory/sell")]
    public Task<ActionResult<CaseOpeningSellResultObj>> SellInventory(
        [FromBody] CaseOpeningSellRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(
            () => _caseOpening.SellCaseOpeningInventory(UserId, request?.OpeningIds ?? [], cancellationToken),
            "sell inventory",
            "selected");
    }

    [HttpPost("daily-drop/claim")]
    public Task<ActionResult<CaseOpeningProgressObj>> ClaimDailyDrop([FromBody] CaseOpeningDailyDropClaimRequest request, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.ClaimCaseOpeningDailyDrop(UserId, request?.RewardKeys ?? [], cancellationToken), "claim daily drop", "daily");
    }

    [HttpPost("daily-drop/upgrades/{upgradeKey}/unlock")]
    public Task<ActionResult<CaseOpeningProgressObj>> UnlockDailyDropUpgrade(string upgradeKey, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.UnlockCaseOpeningDailyDropUpgrade(UserId, upgradeKey, cancellationToken), "unlock daily drop upgrade", upgradeKey);
    }

    [HttpGet("daily-drop/settings")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<int>> GetDailyDropSettings(CancellationToken cancellationToken) => Execute(() => _caseOpening.GetDailyDropRequiredXp(cancellationToken), "load daily drop settings", "daily");
    [HttpPut("daily-drop/settings")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> SetDailyDropSettings([FromBody] CaseOpeningDailyDropSettingsRequest request, CancellationToken cancellationToken) { await _caseOpening.SetDailyDropRequiredXp(request.RequiredXp, cancellationToken); return Ok(new ApiResponse(true, "Daily Drop XP threshold saved.")); }
    [HttpPost("daily-drop/reset")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningProgressObj>> ResetDailyDrop(CancellationToken cancellationToken) => Execute(() => _caseOpening.ResetDailyDrop(UserId, cancellationToken), "reset daily drop", "daily");

    [HttpPut("inventory/{openingId:guid}/lock")]
    public Task<ActionResult<CaseOpeningInventoryLockObj>> SetInventoryLock(
        Guid openingId,
        [FromBody] CaseOpeningInventoryLockRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(
            () => _caseOpening.SetCaseOpeningInventoryLock(UserId, openingId, request?.IsLocked ?? false, cancellationToken),
            "update inventory lock",
            openingId.ToString());
    }

    [HttpGet("inventory/upgrades")]
    public Task<ActionResult<CaseOpeningInventoryUpgradeObj>> GetInventoryUpgrades(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningInventoryUpgrades(UserId, cancellationToken), "load inventory upgrades", "all");
    }

    [HttpPost("inventory/upgrades/{upgradeKey}/unlock")]
    public Task<ActionResult<CaseOpeningInventoryUpgradeObj>> UnlockInventoryUpgrade(string upgradeKey, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.UnlockCaseOpeningInventoryUpgrade(UserId, upgradeKey, cancellationToken), "unlock inventory upgrade", upgradeKey);
    }

    [HttpPut("inventory/auto-sell")]
    public Task<ActionResult<CaseOpeningInventoryUpgradeObj>> SetAutoSell([FromBody] CaseOpeningAutoSellPreferenceRequestObj request, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.SetCaseOpeningAutoSellPreference(UserId, request.RarityKey, request.Enabled, request.PreserveStatTrak, cancellationToken), "update auto sell", request.RarityKey);
    }

    [HttpGet("auto-buy/rules")]
    public Task<ActionResult<CaseOpeningAutoBuySummaryObj>> GetAutoBuyRules(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningAutoBuyRules(UserId, cancellationToken), "load auto-buy rules", "all");
    }

    [HttpPut("auto-buy/rules/{caseKey}")]
    public Task<ActionResult<CaseOpeningAutoBuySummaryObj>> SetAutoBuyRule(
        string caseKey,
        [FromBody] CaseOpeningAutoBuyRuleRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.SetCaseOpeningAutoBuyRule(UserId, caseKey, request, cancellationToken), "save auto-buy rule", caseKey);
    }

    [HttpDelete("auto-buy/rules/{caseKey}")]
    public Task<ActionResult<CaseOpeningAutoBuySummaryObj>> DeleteAutoBuyRule(string caseKey, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.DeleteCaseOpeningAutoBuyRule(UserId, caseKey, cancellationToken), "delete auto-buy rule", caseKey);
    }

    [HttpPost("auto-buy/evaluate")]
    public Task<ActionResult<List<CaseOpeningCasePurchaseResultObj>>> EvaluateAutoBuy(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.EvaluateCaseOpeningAutoBuyRules(UserId, cancellationToken), "evaluate auto-buy", "all");
    }

    [HttpPost("cases/{caseKey}/purchase")]
    public Task<ActionResult<CaseOpeningCasePurchaseResultObj>> PurchaseCases(string caseKey, [FromBody] CaseOpeningCasePurchaseRequestObj? request, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.PurchaseCaseOpeningCases(UserId, caseKey, request?.Quantity ?? 1, cancellationToken), "purchase cases", caseKey);
    }

    [HttpPost("storage-containers")]
    public Task<ActionResult<CaseOpeningStoragePurchaseResultObj>> PurchaseStorageContainer(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.PurchaseCaseOpeningStorageContainer(UserId, cancellationToken), "purchase storage container", "inventory");
    }

    [HttpPost("trade-ups")]
    public Task<ActionResult<CaseOpeningTradeUpResultObj>> CreateTradeUp(
        [FromBody] CaseOpeningTradeUpRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(
            () => _caseOpening.CreateCaseOpeningTradeUp(UserId, request?.OpeningIds ?? [], cancellationToken),
            "create trade up",
            "selected");
    }

    [HttpGet("trade-up-recipes")]
    public Task<ActionResult<CaseOpeningTradeUpRecipeSummaryObj>> GetTradeUpRecipes(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningTradeUpRecipes(UserId, cancellationToken), "load auto trade-up recipes", "all");
    }

    [HttpPost("trade-up-recipes")]
    public Task<ActionResult<CaseOpeningTradeUpRecipeSummaryObj>> CreateTradeUpRecipe(
        [FromBody] CaseOpeningTradeUpRecipeRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.CreateCaseOpeningTradeUpRecipe(UserId, request, cancellationToken), "create auto trade-up recipe", "new");
    }

    [HttpPut("trade-up-recipes/{recipeId:guid}/active")]
    public Task<ActionResult<CaseOpeningTradeUpRecipeSummaryObj>> SetTradeUpRecipeActive(
        Guid recipeId,
        [FromBody] CaseOpeningTradeUpRecipeActiveRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(
            () => _caseOpening.SetCaseOpeningTradeUpRecipeActive(UserId, recipeId, request?.IsActive ?? false, cancellationToken),
            "update auto trade-up recipe",
            recipeId.ToString());
    }

    [HttpDelete("trade-up-recipes/{recipeId:guid}")]
    public Task<ActionResult<CaseOpeningTradeUpRecipeSummaryObj>> DeleteTradeUpRecipe(Guid recipeId, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.DeleteCaseOpeningTradeUpRecipe(UserId, recipeId, cancellationToken), "delete auto trade-up recipe", recipeId.ToString());
    }

    [HttpPost("trade-up-recipes/holdings/{holdingId:guid}/collect")]
    public Task<ActionResult<CaseOpeningTradeUpRecipeSummaryObj>> CollectTradeUpHolding(Guid holdingId, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.CollectCaseOpeningTradeUpHolding(UserId, holdingId, cancellationToken), "collect held skin", holdingId.ToString());
    }

    [HttpPost("trade-up-recipes/slots/upgrade")]
    public Task<ActionResult<CaseOpeningInventoryUpgradeObj>> UpgradeTradeUpRecipeSlots(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.UpgradeCaseOpeningTradeUpRecipeSlots(UserId, cancellationToken), "upgrade auto trade-up recipe slots", "all");
    }

    [HttpPost("trade-up-recipes/{recipeId:guid}/holding/upgrade")]
    public Task<ActionResult<CaseOpeningTradeUpRecipeSummaryObj>> UpgradeTradeUpRecipeHolding(Guid recipeId, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.UpgradeCaseOpeningTradeUpRecipeHolding(UserId, recipeId, cancellationToken), "upgrade recipe holding capacity", recipeId.ToString());
    }

    [HttpPost("trade-up-recipes/evaluate")]
    public Task<ActionResult<List<CaseOpeningTradeUpResultObj>>> EvaluateTradeUpRecipes(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.EvaluateCaseOpeningTradeUpRecipes(UserId, cancellationToken), "evaluate auto trade-up recipes", "all");
    }

    [HttpGet("bots")]
    public Task<ActionResult<CaseOpeningBotProgressObj>> GetBots(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseOpeningBotProgress(UserId, cancellationToken), "load bots", "all");
    }

    [HttpPost("bots/servers")]
    public Task<ActionResult<CaseOpeningBotProgressObj>> PurchaseBotServer(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.PurchaseCaseOpeningBotServer(UserId, cancellationToken), "purchase bot server", "all");
    }

    [HttpPost("bots")]
    public Task<ActionResult<CaseOpeningBotProgressObj>> PurchaseBot(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.PurchaseCaseOpeningBot(UserId, cancellationToken), "purchase bot", "all");
    }

    [HttpPost("bots/servers/{serverId:guid}/speed")]
    public Task<ActionResult<CaseOpeningBotProgressObj>> UpgradeBotServerSpeed(Guid serverId, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.UpgradeCaseOpeningBotServer(UserId, serverId, cancellationToken), "upgrade bot server speed", serverId.ToString());
    }

    [HttpPost("bots/{botId:guid}/open")]
    public Task<ActionResult<CaseOpeningResultObj>> OpenBotCase(
        Guid botId,
        [FromBody] CaseOpeningBotOpenRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(
            () => _caseOpening.OpenCaseWithBot(UserId, botId, request?.CaseKey ?? string.Empty, cancellationToken),
            "open bot case",
            request?.CaseKey ?? string.Empty);
    }

    [HttpGet("cases/{caseKey}/statistics")]
    public Task<ActionResult<CaseOpeningStatisticsObj>> GetStatistics(string caseKey, CancellationToken cancellationToken)
    {
        return Execute(
            () => _caseOpening.GetCaseOpeningStatistics(UserId, caseKey, cancellationToken),
            "load statistics",
            caseKey);
    }

    [HttpPost("cases/{caseKey}/open")]
    public Task<ActionResult<CaseOpeningOpenBatchResultObj>> OpenCase(
        string caseKey,
        [FromBody] CaseOpeningOpenRequestObj? request,
        CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.OpenCases(UserId, caseKey, request?.Quantity ?? 1, cancellationToken), "open", caseKey);
    }

    // ---------- Game settings + per-case settings (the variable-tweak modal's "Game settings" tab) ----------

    [HttpGet("settings")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningGameSettingsObj>> GetGameSettings(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetGameSettings(cancellationToken), "load game settings", "all");
    }

    [HttpPut("settings")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningGameSettingsObj>> UpdateGameSettings(
        [FromBody] CaseOpeningGameSettingsObj request,
        CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.SetGameSettings(request, cancellationToken), "update game settings", "all");
    }

    [HttpGet("settings/cases")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<List<CaseOpeningCaseSettingsObj>>> GetCaseSettings(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetCaseSettings(cancellationToken), "load case settings", "all");
    }

    [HttpGet("battle-reactions")]
    public Task<ActionResult<CaseBattleReactionShopObj>> GetBattleReactions(CancellationToken cancellationToken) =>
        Execute(() => _caseOpening.GetCaseBattleReactionShop(UserId, cancellationToken), "load battle reactions", "all");

    [HttpPost("battle-reactions/{reactionKey}/unlock")]
    public Task<ActionResult<CaseBattleReactionShopObj>> UnlockBattleReaction(string reactionKey, CancellationToken cancellationToken) =>
        Execute(() => _caseOpening.PurchaseCaseBattleReaction(UserId, reactionKey, cancellationToken), "unlock battle reaction", reactionKey);

    [HttpPost("cases/{caseKey}/discard")]
    public Task<ActionResult<CaseOpeningCaseDiscardResultObj>> DiscardCases(string caseKey, [FromBody] CaseOpeningCaseDiscardRequestObj? request, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.DiscardCaseOpeningCases(UserId, caseKey, request?.Quantity ?? -1, cancellationToken), "discard cases", caseKey);
    }

    [HttpPost("bots/{botId:guid}/speed")]
    public Task<ActionResult<CaseOpeningBotProgressObj>> UpgradeBotSpeed(Guid botId, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.UpgradeCaseOpeningBot(UserId, botId, cancellationToken), "upgrade bot speed", botId.ToString());
    }

    [HttpPut("bots/servers/{serverId:guid}/enabled")]
    public Task<ActionResult<CaseOpeningBotProgressObj>> SetBotServerEnabled(
        Guid serverId,
        [FromBody] CaseOpeningBotServerStateRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.SetCaseOpeningBotServerEnabled(UserId, serverId, request?.IsEnabled ?? false, cancellationToken), "update bot server", serverId.ToString());
    }

    [HttpPost("bots/cycle")]
    public Task<ActionResult<CaseOpeningBotCycleResultObj>> RunBotCycle(
        [FromBody] CaseOpeningBotOpenRequestObj request,
        CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.RunCaseOpeningBotCycle(UserId, request?.CaseKey ?? string.Empty, cancellationToken), "run bot cycle", request?.CaseKey ?? string.Empty);
    }

    [HttpGet("settings/price-snapshots")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningPriceSnapshotSummaryObj>> GetPriceSnapshots(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetPriceSnapshots(cancellationToken), "load price snapshots", "Skinport");
    }

    [HttpPost("settings/price-snapshots")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningPriceSnapshotSummaryObj>> CreatePriceSnapshot(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.CreatePriceSnapshot(UserId, cancellationToken), "create price snapshot", "Skinport and CSFloat");
    }

    [HttpPost("settings/price-snapshots/{snapshotId:guid}/activate")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningPriceSnapshotSummaryObj>> ActivatePriceSnapshot(Guid snapshotId, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.ActivatePriceSnapshot(snapshotId, cancellationToken), "activate price snapshot", snapshotId.ToString());
    }

    [HttpDelete("settings/price-snapshots/{snapshotId:guid}")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningPriceSnapshotSummaryObj>> DeletePriceSnapshot(Guid snapshotId, CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.DeletePriceSnapshot(snapshotId, cancellationToken), "delete price snapshot", snapshotId.ToString());
    }

    [HttpPost("settings/price-snapshots/publish-balance")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningPriceSnapshotSummaryObj>> PublishPriceSnapshotBalance(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.PublishPriceSnapshotBalance(cancellationToken), "publish price balance", "Skinport");
    }

    [HttpGet("settings/special-variants")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningSpecialVariantAdminSummaryObj>> GetSpecialVariants(CancellationToken cancellationToken)
        => Execute(() => _caseOpening.GetSpecialVariantSettings(cancellationToken), "load special variants", "catalogue");

    [HttpPost("settings/special-variants")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningSpecialVariantAdminSummaryObj>> CreateSpecialVariant([FromBody] CaseOpeningSpecialVariantRuleRequestObj request, CancellationToken cancellationToken)
        => Execute(() => _caseOpening.SaveSpecialVariantRule(null, request, cancellationToken), "create special variant", request?.Name ?? string.Empty);

    [HttpPut("settings/special-variants/{ruleId:guid}")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningSpecialVariantAdminSummaryObj>> UpdateSpecialVariant(Guid ruleId, [FromBody] CaseOpeningSpecialVariantRuleRequestObj request, CancellationToken cancellationToken)
        => Execute(() => _caseOpening.SaveSpecialVariantRule(ruleId, request, cancellationToken), "save special variant", ruleId.ToString());

    [HttpPost("settings/special-variant-price-snapshots")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningSpecialVariantAdminSummaryObj>> CreateSpecialVariantPriceSnapshot([FromBody] CaseOpeningSpecialVariantPriceSnapshotRequestObj request, CancellationToken cancellationToken)
        => Execute(() => _caseOpening.CreateSpecialVariantPriceSnapshot(request, cancellationToken), "create special-variant price snapshot", request?.Name ?? string.Empty);

    [HttpPost("settings/special-variant-price-snapshots/{snapshotId:guid}/activate")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningSpecialVariantAdminSummaryObj>> ActivateSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken)
        => Execute(() => _caseOpening.ActivateSpecialVariantPriceSnapshot(snapshotId, cancellationToken), "activate special-variant price snapshot", snapshotId.ToString());

    [HttpDelete("settings/special-variant-price-snapshots/{snapshotId:guid}")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningSpecialVariantAdminSummaryObj>> DeleteSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken)
        => Execute(() => _caseOpening.DeleteSpecialVariantPriceSnapshot(snapshotId, cancellationToken), "delete special-variant price snapshot", snapshotId.ToString());

    [HttpPost("settings/special-variants/csfloat-import")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<List<CaseOpeningSpecialVariantListingEvidenceObj>>> ImportSpecialVariantListings(CancellationToken cancellationToken)
        => Execute(() => _caseOpening.ImportSpecialVariantListings(UserId, cancellationToken), "import CSFloat listing evidence", "special variants");

    [HttpPut("settings/cases/{caseKey}")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> UpdateCaseSettings(
        string caseKey,
        [FromBody] CaseOpeningCaseSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _caseOpening.SetCaseSettings(caseKey, request.Tier, request.UnlockCostStars, request.UnlockCostGbpPence, request.PurchaseCostStars, request.PurchaseCostGbpPence, request.XpRequirement, cancellationToken);
            return Ok(new ApiResponse(true, "Case settings saved."));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiResponse(false, exception.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update case settings for {CaseKey}.", caseKey);
            return StatusCode(502, new ApiResponse(false, "Case settings could not be saved. Please try again shortly."));
        }
    }

    [HttpGet("settings/tiers")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<List<CaseOpeningTierEconomySettingsObj>>> GetTierEconomySettings(CancellationToken cancellationToken)
        => Execute(() => _caseOpening.GetTierEconomySettings(cancellationToken), "load tier economy settings", "tiers");

    [HttpPut("settings/tiers/{tier:int}")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<List<CaseOpeningTierEconomySettingsObj>>> UpdateTierEconomySettings(
        int tier,
        [FromBody] CaseOpeningTierEconomySettingsObj request,
        CancellationToken cancellationToken)
        => Execute(() => _caseOpening.SetTierEconomySettings(tier, request, cancellationToken), "save tier economy settings", tier.ToString());

    [HttpGet("settings/xp-by-rarity")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<List<CaseOpeningXpByRarityObj>>> GetXpByRarity(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetXpByRarity(cancellationToken), "load xp by rarity", "all");
    }

    [HttpGet("settings/inventory-upgrades")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<List<CaseOpeningUpgradeDefinitionObj>>> GetInventoryUpgradeSettings(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.GetInventoryUpgradeSettings(cancellationToken), "load inventory upgrade settings", "all");
    }

    [HttpPut("settings/inventory-upgrades/{upgradeKey}")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> UpdateInventoryUpgradeSettings(
        string upgradeKey,
        [FromBody] CaseOpeningInventoryUpgradeSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _caseOpening.SetInventoryUpgradeSettings(upgradeKey, request.CostStars, request.CostGbpPence, request.RequiredLevel, cancellationToken);
            return Ok(new ApiResponse(true, "Inventory upgrade settings saved."));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiResponse(false, exception.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update inventory upgrade settings for {UpgradeKey}.", upgradeKey);
            return StatusCode(502, new ApiResponse(false, "The inventory upgrade settings could not be saved."));
        }
    }

    [HttpPut("settings/xp-by-rarity/{rarityKey}")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> UpdateXpByRarity(
        string rarityKey,
        [FromBody] CaseOpeningXpByRarityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _caseOpening.SetXpByRarity(rarityKey, request.XpAwarded, cancellationToken);
            return Ok(new ApiResponse(true, "XP reward saved."));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiResponse(false, exception.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update XP by rarity for {RarityKey}.", rarityKey);
            return StatusCode(502, new ApiResponse(false, "This XP reward could not be saved. Please try again shortly."));
        }
    }

    // ---------- Testing overrides for an administrator-selected account (the control centre's "Your progress" tab) ----------

    [HttpGet("dev/profile")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningDevProfileObj>> GetDevProfile([FromQuery] Guid? targetUserId, CancellationToken cancellationToken)
    {
        return ExecuteForDevTarget(
            targetUserId,
            async userId => new CaseOpeningDevProfileObj(
                await _caseOpening.GetCaseOpeningProgress(userId, cancellationToken),
                await _caseOpening.GetCaseOpeningCases(userId, cancellationToken),
                await _caseOpening.GetDevDropSettings(userId, cancellationToken)),
            "load dev profile",
            "all");
    }

    [HttpPut("dev/progress")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningProgressObj>> SetDevProgress(
        [FromBody] CaseOpeningDevProgressRequest request,
        [FromQuery] Guid? targetUserId,
        CancellationToken cancellationToken)
    {
        return ExecuteForDevTarget(targetUserId, userId => _caseOpening.SetDevProgress(userId, request.Stars, request.GbpPence, request.Xp, cancellationToken), "set dev progress", "all");
    }

    [HttpPut("dev/upgrades")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningProgressObj>> SetDevUpgrades(
        [FromBody] CaseOpeningDevUpgradesRequest request,
        [FromQuery] Guid? targetUserId,
        CancellationToken cancellationToken)
    {
        return ExecuteForDevTarget(
            targetUserId,
            userId => _caseOpening.SetDevUpgrades(userId, request.SkipAnimationUnlocked, request.MultiOpenLevel, request.OpenSpeedLevel, cancellationToken),
            "set dev upgrades",
            "all");
    }

    [HttpPut("dev/cases/{caseKey}")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningProgressObj>> SetDevCaseUnlock(
        string caseKey,
        [FromBody] CaseOpeningDevCaseUnlockRequest request,
        [FromQuery] Guid? targetUserId,
        CancellationToken cancellationToken)
    {
        return ExecuteForDevTarget(targetUserId, userId => _caseOpening.SetDevCaseUnlock(userId, caseKey, request.Unlock, cancellationToken), "set dev case unlock", caseKey);
    }

    [HttpPut("dev/drop-rarities")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningDevDropSettingsObj>> SetDevDropRarities(
        [FromBody] CaseOpeningDevDropSettingsRequest request,
        [FromQuery] Guid? targetUserId,
        CancellationToken cancellationToken)
    {
        return ExecuteForDevTarget(targetUserId, userId => _caseOpening.SetDevDropSettings(userId, request.RarityGroups, cancellationToken), "set dev drop rarities", "all");
    }

    [HttpPost("dev/reset")]
    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    public Task<ActionResult<CaseOpeningProgressObj>> ResetDevProgress(CancellationToken cancellationToken)
    {
        return Execute(() => _caseOpening.ResetDevProgress(UserId, cancellationToken), "reset dev progress", "all");
    }

    private async Task<ActionResult<T>> ExecuteForDevTarget<T>(Guid? targetUserId, Func<Guid, Task<T>> action, string operation, string caseKey)
    {
        try
        {
            Guid resolvedUserId = targetUserId ?? UserId;
            AppUser? targetUser = await _auth.GetUser(resolvedUserId);
            if (targetUser is null || !targetUser.IsActive)
                throw new InvalidOperationException("Choose an active user account.");

            return Ok(await action(resolvedUserId));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiResponse(false, exception.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Case-opening {Operation} failed for administrator {UserId}, target user {TargetUserId}, and case {CaseKey}.", operation, UserId, targetUserId ?? UserId, caseKey);
            return StatusCode(502, new ApiResponse(false, "The case service could not be reached. Please try again shortly."));
        }
    }

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action, string operation, string caseKey)
    {
        try
        {
            return Ok(await action());
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            return StatusCode(499);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiResponse(false, exception.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Case-opening {Operation} failed for user {UserId} and case {CaseKey}.",
                operation,
                UserId,
                caseKey);
            return StatusCode(502, new ApiResponse(false, "The case service could not be reached. Please try again shortly."));
        }
    }
}

public sealed record CaseOpeningCaseSettingsRequest(int Tier, int UnlockCostStars, long UnlockCostGbpPence, int PurchaseCostStars, long PurchaseCostGbpPence, int XpRequirement);
public sealed record CaseOpeningDailyDropClaimRequest(List<string> RewardKeys);
public sealed record CaseOpeningDailyDropSettingsRequest(int RequiredXp);
public sealed record CaseOpeningInventoryUpgradeSettingsRequest(int CostStars, long CostGbpPence, int RequiredLevel);
public sealed record CaseOpeningXpByRarityRequest(int XpAwarded);
public sealed record CaseOpeningDevProgressRequest(int Stars, long GbpPence, int Xp);
public sealed record CaseOpeningDevProfileObj(CaseOpeningProgressObj Progress, List<CaseOpeningCaseSummaryObj> Cases, CaseOpeningDevDropSettingsObj DropSettings);
public sealed record CaseOpeningDevUpgradesRequest(bool SkipAnimationUnlocked, int MultiOpenLevel, int OpenSpeedLevel);
public sealed record CaseOpeningDevCaseUnlockRequest(bool Unlock);
public sealed record CaseOpeningDevDropSettingsRequest(List<string>? RarityGroups);

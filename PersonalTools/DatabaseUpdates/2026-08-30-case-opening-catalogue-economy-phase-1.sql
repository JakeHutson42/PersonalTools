-- Phase 1: safely stage the expanded catalogue and its configurable global return curve.
-- Existing case overrides are preserved by INSERT IGNORE; EV-derived values replace these
-- provisional settings only through the later reviewed publish workflow.
ALTER TABLE CaseOpeningGameSettings
    ADD COLUMN IF NOT EXISTS GlobalReturnMultiplierBasisPoints INT UNSIGNED NOT NULL DEFAULT 10300 AFTER SkinSaleRateBasisPoints;

INSERT IGNORE INTO CaseOpeningCaseSettings
    (CaseKey,Tier,UnlockCostStars,UnlockCostGbpPence,PurchaseCostStars,PurchaseCostGbpPence,XpRequirement,UpdatedUtc)
VALUES
    ('operation-bravo',8,800,80000,25,2500,8,UTC_TIMESTAMP()),('winter-offensive',5,175,17500,8,800,4,UTC_TIMESTAMP()),
    ('operation-phoenix',5,175,17500,8,800,4,UTC_TIMESTAMP()),('huntsman',6,300,30000,12,1200,5,UTC_TIMESTAMP()),
    ('operation-vanguard',5,175,17500,8,800,4,UTC_TIMESTAMP()),('chroma',5,175,17500,8,800,4,UTC_TIMESTAMP()),
    ('falchion',4,100,10000,5,500,3,UTC_TIMESTAMP()),('shadow',4,100,10000,5,500,3,UTC_TIMESTAMP()),
    ('revolver',4,100,10000,5,500,3,UTC_TIMESTAMP()),('operation-wildfire',6,300,30000,12,1200,5,UTC_TIMESTAMP()),
    ('chroma-3',4,100,10000,5,500,3,UTC_TIMESTAMP()),('gamma',5,175,17500,8,800,4,UTC_TIMESTAMP()),
    ('spectrum',5,175,17500,8,800,4,UTC_TIMESTAMP()),('horizon',3,50,5000,3,300,2,UTC_TIMESTAMP()),
    ('danger-zone',3,50,5000,3,300,2,UTC_TIMESTAMP()),('community-sticker-capsule-1',7,500,50000,18,1800,6,UTC_TIMESTAMP()),
    ('dreamhack-2014-legends',9,1500,150000,40,4000,10,UTC_TIMESTAMP()),('katowice-2015-legends',9,1500,150000,40,4000,10,UTC_TIMESTAMP()),
    ('enfu',3,50,5000,3,300,2,UTC_TIMESTAMP()),('cologne-2015-legends',8,800,80000,25,2500,8,UTC_TIMESTAMP()),
    ('cluj-napoca-2015-legends',8,800,80000,25,2500,8,UTC_TIMESTAMP()),('pinups',2,25,2500,2,200,1,UTC_TIMESTAMP()),
    ('team-roles',2,25,2500,2,200,1,UTC_TIMESTAMP()),('mlg-columbus-2016-legends',8,800,80000,25,2500,8,UTC_TIMESTAMP()),
    ('cologne-2016-legends',8,800,80000,25,2500,8,UTC_TIMESTAMP()),('krakow-2017-legends',8,800,80000,25,2500,8,UTC_TIMESTAMP()),
    ('chicken',2,25,2500,2,200,1,UTC_TIMESTAMP()),('boston-2018-legends',7,500,50000,18,1800,6,UTC_TIMESTAMP()),
    ('london-2018-legends',7,500,50000,18,1800,6,UTC_TIMESTAMP()),('katowice-2019-legends',7,500,50000,18,1800,6,UTC_TIMESTAMP()),
    ('halo',2,25,2500,2,200,1,UTC_TIMESTAMP()),('warhammer-40000',3,50,5000,3,300,2,UTC_TIMESTAMP()),
    ('berlin-2019-legends',6,300,30000,12,1200,5,UTC_TIMESTAMP()),('cs20-stickers',2,25,2500,2,200,1,UTC_TIMESTAMP()),
    ('half-life-alyx',2,25,2500,2,200,1,UTC_TIMESTAMP()),('rmr-2020-legends',3,50,5000,3,300,2,UTC_TIMESTAMP()),
    ('poorly-drawn',2,25,2500,2,200,1,UTC_TIMESTAMP()),('ten-year-birthday',2,25,2500,2,200,1,UTC_TIMESTAMP()),
    ('rio-2022-legends',3,50,5000,3,300,2,UTC_TIMESTAMP()),('espionage',3,50,5000,3,300,2,UTC_TIMESTAMP()),
    ('ambush',2,25,2500,2,200,1,UTC_TIMESTAMP()),('shanghai-2024-legends',2,25,2500,2,200,1,UTC_TIMESTAMP()),
    ('budapest-2025-legends',2,25,2500,2,200,1,UTC_TIMESTAMP()),('warhammer-traitor-astartes',2,25,2500,2,200,1,UTC_TIMESTAMP()),
    ('warhammer-adeptus-astartes',2,25,2500,2,200,1,UTC_TIMESTAMP());

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_game_settings_get//
CREATE PROCEDURE sp_case_opening_game_settings_get()
BEGIN
    SELECT EconomyMode,SkinSaleRateBasisPoints,GlobalReturnMultiplierBasisPoints,FreeCaseAllowanceEnabled,FreeCaseAllowanceQuantity,FreeCaseAllowanceHours,
           XpPerCaseOpen,SkipAnimationCostStars,SkipAnimationCostGbpPence,SkipAnimationXpRequirement,
           MultiOpenCostStars,MultiOpenCostGbpPence,MultiOpenXpRequirement,
           OpenSpeedUpgradeBaseCostStars,OpenSpeedUpgradeBaseCostGbpPence,
           OpenSpeedUpgradeCostIncrementStars,OpenSpeedUpgradeCostIncrementGbpPence,OpenSpeedUpgradeXpRequirement,
           MaximumOpenSpeedLevel,MaximumMultiOpenLevel,MaximumOpenQuantity,BotOpeningIntervalSeconds,
           BotServerBaseCostStars,BotServerBaseCostGbpPence,BotServerCostIncrementStars,BotServerCostIncrementGbpPence,
           BotBaseCostStars,BotBaseCostGbpPence,BotSpeedUpgradeBaseCostGbpPence,BotSpeedUpgradeCostIncrementGbpPence,BotCostGrowthRate,
           StorageContainerBaseCostStars,StorageContainerBaseCostGbpPence,StorageContainerCostIncrementStars,StorageContainerCostIncrementGbpPence,
           StorageContainerSlots,MaximumStorageContainers,TradeUpRecipeCostStars,TradeUpRecipeCostGbpPence,
           TradeUpSlotUpgradeBaseCostStars,TradeUpSlotUpgradeCostIncrementStars,TradeUpSlotUpgradeBaseCostGbpPence,TradeUpSlotUpgradeCostIncrementGbpPence,
           TradeUpHoldingUpgradeBaseCostStars,TradeUpHoldingUpgradeCostIncrementStars,TradeUpHoldingUpgradeBaseCostGbpPence,TradeUpHoldingUpgradeCostIncrementGbpPence
    FROM CaseOpeningGameSettings WHERE Id=1;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_global_return_multiplier_set//
CREATE PROCEDURE sp_case_opening_global_return_multiplier_set(IN p_basis_points INT)
BEGIN
    UPDATE CaseOpeningGameSettings
    SET GlobalReturnMultiplierBasisPoints=LEAST(20000,GREATEST(5000,p_basis_points)),UpdatedUtc=UTC_TIMESTAMP()
    WHERE Id=1;
END//
DELIMITER ;

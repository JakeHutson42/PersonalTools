-- Dual Star / simulated GBP economy foundation. GBP values are always stored as integer pence.
ALTER TABLE CaseOpeningProgress ADD COLUMN IF NOT EXISTS GbpPence BIGINT NOT NULL DEFAULT 0 AFTER Stars;
ALTER TABLE CaseOpeningGameSettings
    ADD COLUMN IF NOT EXISTS EconomyMode VARCHAR(10) NOT NULL DEFAULT 'stars' AFTER Id,
    ADD COLUMN IF NOT EXISTS SkinSaleRateBasisPoints INT UNSIGNED NOT NULL DEFAULT 9250 AFTER EconomyMode,
    ADD COLUMN IF NOT EXISTS FreeCaseAllowanceEnabled TINYINT(1) NOT NULL DEFAULT 0 AFTER SkinSaleRateBasisPoints,
    ADD COLUMN IF NOT EXISTS FreeCaseAllowanceQuantity INT UNSIGNED NOT NULL DEFAULT 25 AFTER FreeCaseAllowanceEnabled,
    ADD COLUMN IF NOT EXISTS FreeCaseAllowanceHours INT UNSIGNED NOT NULL DEFAULT 24 AFTER FreeCaseAllowanceQuantity,
    ADD COLUMN IF NOT EXISTS SkipAnimationCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 500 AFTER SkipAnimationCostStars,
    ADD COLUMN IF NOT EXISTS MultiOpenCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 1000 AFTER MultiOpenCostStars,
    ADD COLUMN IF NOT EXISTS OpenSpeedUpgradeBaseCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 250 AFTER OpenSpeedUpgradeBaseCostStars,
    ADD COLUMN IF NOT EXISTS OpenSpeedUpgradeCostIncrementGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 250 AFTER OpenSpeedUpgradeCostIncrementStars,
    ADD COLUMN IF NOT EXISTS BotServerBaseCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 2500 AFTER BotServerBaseCostStars,
    ADD COLUMN IF NOT EXISTS BotServerCostIncrementGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 2500 AFTER BotServerCostIncrementStars,
    ADD COLUMN IF NOT EXISTS BotBaseCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 600 AFTER BotBaseCostStars,
    ADD COLUMN IF NOT EXISTS BotSpeedUpgradeBaseCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 300 AFTER BotBaseCostGbpPence,
    ADD COLUMN IF NOT EXISTS BotSpeedUpgradeCostIncrementGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 100 AFTER BotSpeedUpgradeBaseCostGbpPence,
    ADD COLUMN IF NOT EXISTS StorageContainerBaseCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 1500 AFTER StorageContainerBaseCostStars,
    ADD COLUMN IF NOT EXISTS StorageContainerCostIncrementGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 750 AFTER StorageContainerCostIncrementStars,
    ADD COLUMN IF NOT EXISTS TradeUpRecipeCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 750 AFTER TradeUpRecipeCostStars,
    ADD COLUMN IF NOT EXISTS TradeUpSlotUpgradeBaseCostStars INT UNSIGNED NOT NULL DEFAULT 300 AFTER TradeUpRecipeCostGbpPence,
    ADD COLUMN IF NOT EXISTS TradeUpSlotUpgradeCostIncrementStars INT UNSIGNED NOT NULL DEFAULT 75 AFTER TradeUpSlotUpgradeBaseCostStars,
    ADD COLUMN IF NOT EXISTS TradeUpSlotUpgradeBaseCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 300 AFTER TradeUpSlotUpgradeCostIncrementStars,
    ADD COLUMN IF NOT EXISTS TradeUpSlotUpgradeCostIncrementGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 75 AFTER TradeUpSlotUpgradeBaseCostGbpPence,
    ADD COLUMN IF NOT EXISTS TradeUpHoldingUpgradeBaseCostStars INT UNSIGNED NOT NULL DEFAULT 250 AFTER TradeUpSlotUpgradeCostIncrementGbpPence,
    ADD COLUMN IF NOT EXISTS TradeUpHoldingUpgradeCostIncrementStars INT UNSIGNED NOT NULL DEFAULT 50 AFTER TradeUpHoldingUpgradeBaseCostStars,
    ADD COLUMN IF NOT EXISTS TradeUpHoldingUpgradeBaseCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 250 AFTER TradeUpHoldingUpgradeCostIncrementStars,
    ADD COLUMN IF NOT EXISTS TradeUpHoldingUpgradeCostIncrementGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 50 AFTER TradeUpHoldingUpgradeBaseCostGbpPence;

ALTER TABLE CaseOpeningCaseSettings
    ADD COLUMN IF NOT EXISTS Tier TINYINT UNSIGNED NOT NULL DEFAULT 1 AFTER CaseKey,
    ADD COLUMN IF NOT EXISTS UnlockCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 0 AFTER UnlockCostStars,
    ADD COLUMN IF NOT EXISTS PurchaseCostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 0 AFTER PurchaseCostStars;
ALTER TABLE CaseOpeningUpgradeDefinitions ADD COLUMN IF NOT EXISTS CostGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 0 AFTER CostStars;
UPDATE CaseOpeningUpgradeDefinitions SET CostGbpPence=CostStars WHERE CostGbpPence=0 AND CostStars>0;
CREATE TABLE IF NOT EXISTS CaseOpeningUserUpgradeUnlocks(UserId CHAR(36) NOT NULL,UpgradeKey VARCHAR(50) NOT NULL,UnlockedUtc DATETIME(6) NOT NULL,PRIMARY KEY(UserId,UpgradeKey),CONSTRAINT FK_CaseOpeningUserUpgradeUnlocks_User FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE,CONSTRAINT FK_CaseOpeningUserUpgradeUnlocks_Definition FOREIGN KEY(UpgradeKey) REFERENCES CaseOpeningUpgradeDefinitions(UpgradeKey) ON DELETE CASCADE) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;
INSERT IGNORE INTO CaseOpeningUserUpgradeUnlocks(UserId,UpgradeKey,UnlockedUtc)
SELECT u.UserId,d.UpgradeKey,UTC_TIMESTAMP(6) FROM CaseOpeningUserInventoryUpgrades u JOIN CaseOpeningUpgradeDefinitions d ON
 (d.UpgradeKey='bulk-sell-200' AND u.BulkSellLimit>=200) OR (d.UpgradeKey='bulk-sell-300' AND u.BulkSellLimit>=300) OR (d.UpgradeKey='bulk-sell-400' AND u.BulkSellLimit>=400) OR (d.UpgradeKey='bulk-sell-500' AND u.BulkSellLimit>=500) OR
 (d.UpgradeKey='auto-sell-covert' AND u.AutoSellCovertUnlocked=1) OR (d.UpgradeKey='auto-sell-classified' AND u.AutoSellClassifiedUnlocked=1) OR (d.UpgradeKey='auto-sell-restricted' AND u.AutoSellRestrictedUnlocked=1) OR (d.UpgradeKey='auto-sell-mil-spec' AND u.AutoSellMilSpecUnlocked=1) OR
 (d.UpgradeKey='inventory-slots-250' AND u.BonusInventorySlots>=250) OR (d.UpgradeKey='inventory-slots-500' AND u.BonusInventorySlots>=750) OR (d.UpgradeKey='inventory-slots-1000' AND u.BonusInventorySlots>=1750) OR
 (d.UpgradeKey='auto-buy-unlock' AND u.AutoBuyUnlocked=1) OR (d.UpgradeKey='auto-buy-slots-5' AND u.AutoBuyRuleSlots>=5) OR (d.UpgradeKey='auto-buy-slots-10' AND u.AutoBuyRuleSlots>=10) OR (d.UpgradeKey='trade-up-unlock' AND u.TradeUpRecipesUnlocked=1);
ALTER TABLE CaseOpeningAchievementDefinitions ADD COLUMN IF NOT EXISTS RewardGbpPence BIGINT UNSIGNED NOT NULL DEFAULT 0 AFTER RewardStars;
UPDATE CaseOpeningAchievementDefinitions SET RewardGbpPence=RewardStars*5 WHERE RewardGbpPence=0 AND RewardStars>0;

UPDATE CaseOpeningCaseSettings SET Tier=CASE
    WHEN UnlockCostStars<=10 THEN 1 WHEN UnlockCostStars<=25 THEN 2 WHEN UnlockCostStars<=50 THEN 3
    WHEN UnlockCostStars<=100 THEN 4 WHEN UnlockCostStars<=175 THEN 5 WHEN UnlockCostStars<=300 THEN 6
    WHEN UnlockCostStars<=500 THEN 7 WHEN UnlockCostStars<=800 THEN 8 WHEN UnlockCostStars<=1500 THEN 9 ELSE 10 END;
-- Split the four top legacy cases across the final two tiers so every tier has a meaningful catalogue.
UPDATE CaseOpeningCaseSettings SET Tier=10 WHERE CaseKey IN ('katowice-2014-legends','cologne-2014-cobblestone-souvenir');

CREATE TABLE IF NOT EXISTS CaseOpeningTierEconomySettings(
    Tier TINYINT UNSIGNED NOT NULL,TargetProfitBasisPoints INT UNSIGNED NOT NULL,
    PriceRoundingPence INT UNSIGNED NOT NULL DEFAULT 5,UpdatedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY(Tier)
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;
INSERT INTO CaseOpeningTierEconomySettings(Tier,TargetProfitBasisPoints,PriceRoundingPence,UpdatedUtc) VALUES
    (1,0,1,UTC_TIMESTAMP(6)),(2,200,5,UTC_TIMESTAMP(6)),(3,300,5,UTC_TIMESTAMP(6)),
    (4,400,5,UTC_TIMESTAMP(6)),(5,500,10,UTC_TIMESTAMP(6)),(6,600,10,UTC_TIMESTAMP(6)),
    (7,700,25,UTC_TIMESTAMP(6)),(8,800,25,UTC_TIMESTAMP(6)),(9,900,50,UTC_TIMESTAMP(6)),
    (10,1000,100,UTC_TIMESTAMP(6))
ON DUPLICATE KEY UPDATE Tier=VALUES(Tier);

CREATE TABLE IF NOT EXISTS CaseOpeningEconomyLedger(
    TransactionId CHAR(36) NOT NULL,UserId CHAR(36) NOT NULL,EconomyMode VARCHAR(10) NOT NULL,
    AmountMinor BIGINT NOT NULL,BalanceAfterMinor BIGINT NOT NULL,TransactionType VARCHAR(50) NOT NULL,
    ReferenceType VARCHAR(40) NULL,ReferenceId VARCHAR(160) NULL,PriceSnapshotId CHAR(36) NULL,
    CreatedUtc DATETIME(6) NOT NULL,PRIMARY KEY(TransactionId),
    KEY IX_CaseOpeningEconomyLedger_UserCreated(UserId,CreatedUtc),
    CONSTRAINT FK_CaseOpeningEconomyLedger_Users FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS CaseOpeningUserFreeCaseAllowances(
    UserId CHAR(36) NOT NULL,WindowStartedUtc DATETIME(6) NOT NULL,QuantityClaimed INT UNSIGNED NOT NULL DEFAULT 0,
    PRIMARY KEY(UserId),CONSTRAINT FK_CaseOpeningUserFreeCaseAllowances_Users FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_progress_get//
DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_active_items_get//
CREATE PROCEDURE sp_case_opening_price_snapshot_active_items_get()
BEGIN
    WITH ranked AS (
        SELECT i.PriceSnapshotId,i.MarketHashName,i.Price,i.MinimumPrice,i.MeanPrice,i.MedianPrice,i.SuggestedPrice,i.Quantity,i.SourceUpdatedUtc,s.IsActive,
               ROW_NUMBER() OVER(PARTITION BY i.MarketHashName ORDER BY s.IsActive DESC,s.ImportedUtc DESC) rn
        FROM CaseOpeningPriceSnapshotItems i JOIN CaseOpeningPriceSnapshots s ON s.PriceSnapshotId=i.PriceSnapshotId
        CROSS JOIN (SELECT ImportedUtc FROM CaseOpeningPriceSnapshots WHERE IsActive=1 LIMIT 1) active
        WHERE s.ImportedUtc<=active.ImportedUtc
    )
    SELECT PriceSnapshotId,MarketHashName,Price,MinimumPrice,MeanPrice,MedianPrice,SuggestedPrice,Quantity,SourceUpdatedUtc,IF(IsActive=1 AND MedianPrice IS NOT NULL,0,1) IsFallback FROM ranked WHERE rn=1 ORDER BY MarketHashName;
END//
CREATE PROCEDURE sp_case_opening_progress_get(IN p_user_id CHAR(36))
BEGIN
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc)
    VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
    SELECT UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel
    FROM CaseOpeningProgress WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_game_settings_get//
CREATE PROCEDURE sp_case_opening_game_settings_get()
BEGIN
    SELECT EconomyMode,SkinSaleRateBasisPoints,FreeCaseAllowanceEnabled,FreeCaseAllowanceQuantity,FreeCaseAllowanceHours,
           XpPerCaseOpen,SkipAnimationCostStars,SkipAnimationCostGbpPence,SkipAnimationXpRequirement,
           MultiOpenCostStars,MultiOpenCostGbpPence,MultiOpenXpRequirement,
           OpenSpeedUpgradeBaseCostStars,OpenSpeedUpgradeBaseCostGbpPence,
           OpenSpeedUpgradeCostIncrementStars,OpenSpeedUpgradeCostIncrementGbpPence,OpenSpeedUpgradeXpRequirement,
           MaximumOpenSpeedLevel,MaximumMultiOpenLevel,MaximumOpenQuantity,BotOpeningIntervalSeconds,
           BotServerBaseCostStars,BotServerBaseCostGbpPence,BotServerCostIncrementStars,BotServerCostIncrementGbpPence,
           BotBaseCostStars,BotBaseCostGbpPence,BotSpeedUpgradeBaseCostGbpPence,BotSpeedUpgradeCostIncrementGbpPence,BotCostGrowthRate,
           StorageContainerBaseCostStars,StorageContainerBaseCostGbpPence,
           StorageContainerCostIncrementStars,StorageContainerCostIncrementGbpPence,
           StorageContainerSlots,MaximumStorageContainers,TradeUpRecipeCostStars,TradeUpRecipeCostGbpPence,
           TradeUpSlotUpgradeBaseCostStars,TradeUpSlotUpgradeCostIncrementStars,TradeUpSlotUpgradeBaseCostGbpPence,TradeUpSlotUpgradeCostIncrementGbpPence,
           TradeUpHoldingUpgradeBaseCostStars,TradeUpHoldingUpgradeCostIncrementStars,TradeUpHoldingUpgradeBaseCostGbpPence,TradeUpHoldingUpgradeCostIncrementGbpPence
    FROM CaseOpeningGameSettings WHERE Id=1;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_game_settings_set//
CREATE PROCEDURE sp_case_opening_game_settings_set(
    IN p_economy_mode VARCHAR(10),IN p_skin_sale_rate_basis_points INT,IN p_free_case_allowance_enabled TINYINT,
    IN p_free_case_allowance_quantity INT,IN p_free_case_allowance_hours INT,IN p_xp_per_case_open INT,
    IN p_skip_animation_cost_stars INT,IN p_skip_animation_cost_gbp_pence BIGINT,IN p_skip_animation_xp_requirement INT,
    IN p_multi_open_cost_stars INT,IN p_multi_open_cost_gbp_pence BIGINT,IN p_multi_open_xp_requirement INT,
    IN p_open_speed_upgrade_base_cost_stars INT,IN p_open_speed_upgrade_base_cost_gbp_pence BIGINT,
    IN p_open_speed_upgrade_cost_increment_stars INT,IN p_open_speed_upgrade_cost_increment_gbp_pence BIGINT,
    IN p_open_speed_upgrade_xp_requirement INT,IN p_maximum_open_speed_level TINYINT UNSIGNED,
    IN p_maximum_multi_open_level TINYINT UNSIGNED,IN p_maximum_open_quantity TINYINT UNSIGNED,
    IN p_bot_opening_interval_seconds INT,IN p_bot_server_base_cost_stars INT,IN p_bot_server_base_cost_gbp_pence BIGINT,
    IN p_bot_server_cost_increment_stars INT,IN p_bot_server_cost_increment_gbp_pence BIGINT,
    IN p_bot_base_cost_stars INT,IN p_bot_base_cost_gbp_pence BIGINT,IN p_bot_speed_upgrade_base_cost_gbp_pence BIGINT,IN p_bot_speed_upgrade_cost_increment_gbp_pence BIGINT,IN p_bot_cost_growth_rate DECIMAL(5,3),
    IN p_storage_container_base_cost_stars INT,IN p_storage_container_base_cost_gbp_pence BIGINT,
    IN p_storage_container_cost_increment_stars INT,IN p_storage_container_cost_increment_gbp_pence BIGINT,
    IN p_storage_container_slots INT,IN p_maximum_storage_containers INT,
    IN p_trade_up_recipe_cost_stars INT,IN p_trade_up_recipe_cost_gbp_pence BIGINT,
    IN p_trade_up_slot_upgrade_base_cost_stars INT,IN p_trade_up_slot_upgrade_cost_increment_stars INT,
    IN p_trade_up_slot_upgrade_base_cost_gbp_pence BIGINT,IN p_trade_up_slot_upgrade_cost_increment_gbp_pence BIGINT,
    IN p_trade_up_holding_upgrade_base_cost_stars INT,IN p_trade_up_holding_upgrade_cost_increment_stars INT,
    IN p_trade_up_holding_upgrade_base_cost_gbp_pence BIGINT,IN p_trade_up_holding_upgrade_cost_increment_gbp_pence BIGINT)
BEGIN
    UPDATE CaseOpeningGameSettings SET EconomyMode=IF(p_economy_mode='gbp','gbp','stars'),
        SkinSaleRateBasisPoints=LEAST(10000,GREATEST(0,p_skin_sale_rate_basis_points)),
        FreeCaseAllowanceEnabled=IF(p_free_case_allowance_enabled<>0,1,0),
        FreeCaseAllowanceQuantity=GREATEST(1,p_free_case_allowance_quantity),FreeCaseAllowanceHours=GREATEST(1,p_free_case_allowance_hours),
        XpPerCaseOpen=p_xp_per_case_open,SkipAnimationCostStars=p_skip_animation_cost_stars,
        SkipAnimationCostGbpPence=GREATEST(0,p_skip_animation_cost_gbp_pence),SkipAnimationXpRequirement=p_skip_animation_xp_requirement,
        MultiOpenCostStars=p_multi_open_cost_stars,MultiOpenCostGbpPence=GREATEST(0,p_multi_open_cost_gbp_pence),MultiOpenXpRequirement=p_multi_open_xp_requirement,
        OpenSpeedUpgradeBaseCostStars=p_open_speed_upgrade_base_cost_stars,OpenSpeedUpgradeBaseCostGbpPence=GREATEST(0,p_open_speed_upgrade_base_cost_gbp_pence),
        OpenSpeedUpgradeCostIncrementStars=p_open_speed_upgrade_cost_increment_stars,OpenSpeedUpgradeCostIncrementGbpPence=GREATEST(0,p_open_speed_upgrade_cost_increment_gbp_pence),
        OpenSpeedUpgradeXpRequirement=p_open_speed_upgrade_xp_requirement,MaximumOpenSpeedLevel=p_maximum_open_speed_level,
        MaximumMultiOpenLevel=p_maximum_multi_open_level,MaximumOpenQuantity=p_maximum_open_quantity,
        BotOpeningIntervalSeconds=p_bot_opening_interval_seconds,BotServerBaseCostStars=p_bot_server_base_cost_stars,
        BotServerBaseCostGbpPence=GREATEST(0,p_bot_server_base_cost_gbp_pence),BotServerCostIncrementStars=p_bot_server_cost_increment_stars,
        BotServerCostIncrementGbpPence=GREATEST(0,p_bot_server_cost_increment_gbp_pence),BotBaseCostStars=p_bot_base_cost_stars,
        BotBaseCostGbpPence=GREATEST(0,p_bot_base_cost_gbp_pence),BotSpeedUpgradeBaseCostGbpPence=GREATEST(0,p_bot_speed_upgrade_base_cost_gbp_pence),BotSpeedUpgradeCostIncrementGbpPence=GREATEST(0,p_bot_speed_upgrade_cost_increment_gbp_pence),BotCostGrowthRate=p_bot_cost_growth_rate,
        StorageContainerBaseCostStars=p_storage_container_base_cost_stars,StorageContainerBaseCostGbpPence=GREATEST(0,p_storage_container_base_cost_gbp_pence),
        StorageContainerCostIncrementStars=p_storage_container_cost_increment_stars,StorageContainerCostIncrementGbpPence=GREATEST(0,p_storage_container_cost_increment_gbp_pence),
        StorageContainerSlots=p_storage_container_slots,MaximumStorageContainers=p_maximum_storage_containers,
        TradeUpRecipeCostStars=p_trade_up_recipe_cost_stars,TradeUpRecipeCostGbpPence=GREATEST(0,p_trade_up_recipe_cost_gbp_pence),
        TradeUpSlotUpgradeBaseCostStars=GREATEST(0,p_trade_up_slot_upgrade_base_cost_stars),TradeUpSlotUpgradeCostIncrementStars=GREATEST(0,p_trade_up_slot_upgrade_cost_increment_stars),
        TradeUpSlotUpgradeBaseCostGbpPence=GREATEST(0,p_trade_up_slot_upgrade_base_cost_gbp_pence),TradeUpSlotUpgradeCostIncrementGbpPence=GREATEST(0,p_trade_up_slot_upgrade_cost_increment_gbp_pence),
        TradeUpHoldingUpgradeBaseCostStars=GREATEST(0,p_trade_up_holding_upgrade_base_cost_stars),TradeUpHoldingUpgradeCostIncrementStars=GREATEST(0,p_trade_up_holding_upgrade_cost_increment_stars),
        TradeUpHoldingUpgradeBaseCostGbpPence=GREATEST(0,p_trade_up_holding_upgrade_base_cost_gbp_pence),TradeUpHoldingUpgradeCostIncrementGbpPence=GREATEST(0,p_trade_up_holding_upgrade_cost_increment_gbp_pence),UpdatedUtc=UTC_TIMESTAMP()
    WHERE Id=1;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_case_settings_get_all//
CREATE PROCEDURE sp_case_opening_case_settings_get_all()
BEGIN
    SELECT CaseKey,Tier,UnlockCostStars,UnlockCostGbpPence,PurchaseCostStars,PurchaseCostGbpPence,XpRequirement
    FROM CaseOpeningCaseSettings ORDER BY Tier,UnlockCostStars,CaseKey;
END//
DROP PROCEDURE IF EXISTS sp_case_opening_case_settings_set//
CREATE PROCEDURE sp_case_opening_case_settings_set(IN p_case_key VARCHAR(80),IN p_tier INT,IN p_unlock_cost_stars INT,IN p_unlock_cost_gbp_pence BIGINT,IN p_purchase_cost_stars INT,IN p_purchase_cost_gbp_pence BIGINT,IN p_xp_requirement INT)
BEGIN
    INSERT INTO CaseOpeningCaseSettings(CaseKey,Tier,UnlockCostStars,UnlockCostGbpPence,PurchaseCostStars,PurchaseCostGbpPence,XpRequirement,UpdatedUtc)
    VALUES(p_case_key,LEAST(10,GREATEST(1,p_tier)),p_unlock_cost_stars,p_unlock_cost_gbp_pence,p_purchase_cost_stars,p_purchase_cost_gbp_pence,p_xp_requirement,UTC_TIMESTAMP())
    ON DUPLICATE KEY UPDATE Tier=VALUES(Tier),UnlockCostStars=VALUES(UnlockCostStars),UnlockCostGbpPence=VALUES(UnlockCostGbpPence),PurchaseCostStars=VALUES(PurchaseCostStars),PurchaseCostGbpPence=VALUES(PurchaseCostGbpPence),XpRequirement=VALUES(XpRequirement),UpdatedUtc=UTC_TIMESTAMP();
END//

DROP PROCEDURE IF EXISTS sp_case_opening_case_unlock//
CREATE PROCEDURE sp_case_opening_case_unlock(IN p_user_id CHAR(36),IN p_case_key VARCHAR(80),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1;
    SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars);
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
    IF EXISTS(SELECT 1 FROM CaseOpeningUnlockedCases WHERE UserId=p_user_id AND CaseKey=p_case_key) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This case is already unlocked.'; END IF;
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency to unlock this case.'; END IF;
    INSERT INTO CaseOpeningUnlockedCases(UserId,CaseKey,UnlockedUtc) VALUES(p_user_id,p_case_key,UTC_TIMESTAMP());
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'case-unlock','case',p_case_key,NULL,UTC_TIMESTAMP(6));
    COMMIT;
    SELECT UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel FROM CaseOpeningProgress WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_upgrade_unlock//
CREATE PROCEDURE sp_case_opening_upgrade_unlock(IN p_user_id CHAR(36),IN p_upgrade_key VARCHAR(30),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT,IN p_max_multi_open_level TINYINT UNSIGNED,IN p_max_open_speed_level TINYINT UNSIGNED)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1;
    SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars);
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),
      MultiOpenLevel=IF(p_upgrade_key='multi-open',MultiOpenLevel+1,MultiOpenLevel),OpenSpeedLevel=IF(p_upgrade_key='open-speed',OpenSpeedLevel+1,OpenSpeedLevel),
      SkipAnimationUnlocked=IF(p_upgrade_key='open-speed' AND OpenSpeedLevel+1>=p_max_open_speed_level,1,SkipAnimationUnlocked),UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost AND ((p_upgrade_key='multi-open' AND MultiOpenLevel<p_max_multi_open_level) OR (p_upgrade_key='open-speed' AND OpenSpeedLevel<p_max_open_speed_level));
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The upgrade is complete or there is not enough currency.'; END IF;
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'opening-upgrade','upgrade',p_upgrade_key,NULL,UTC_TIMESTAMP(6));
    SELECT UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel FROM CaseOpeningProgress WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_cases_purchase//
CREATE PROCEDURE sp_case_opening_cases_purchase(IN p_user_id CHAR(36),IN p_case_key VARCHAR(80),IN p_quantity INT,IN p_purchase_cost_stars INT,IN p_purchase_cost_gbp_pence BIGINT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_unit_cost BIGINT; DECLARE v_total_cost BIGINT;
    DECLARE v_allowance_enabled TINYINT DEFAULT 0; DECLARE v_allowance_quantity INT DEFAULT 25; DECLARE v_allowance_hours INT DEFAULT 24; DECLARE v_claimed INT DEFAULT 0; DECLARE v_window DATETIME(6);
    DECLARE v_base_capacity INT DEFAULT 1000; DECLARE v_storage_slots INT DEFAULT 0; DECLARE v_upgrade_slots INT DEFAULT 0; DECLARE v_skin_slots INT DEFAULT 0; DECLARE v_case_slots INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode,FreeCaseAllowanceEnabled,FreeCaseAllowanceQuantity,FreeCaseAllowanceHours INTO v_mode,v_allowance_enabled,v_allowance_quantity,v_allowance_hours FROM CaseOpeningGameSettings WHERE Id=1; SET v_unit_cost=IF(v_mode='gbp',p_purchase_cost_gbp_pence,p_purchase_cost_stars); SET v_total_cost=p_quantity*v_unit_cost;
    START TRANSACTION;
    IF p_quantity<1 OR p_quantity>500 OR v_unit_cost<0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Buy between 1 and 500 cases at a time.'; END IF;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningUnlockedCases WHERE UserId=p_user_id AND CaseKey=p_case_key) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Unlock this case before buying it.'; END IF;
    IF v_unit_cost=0 AND v_allowance_enabled=1 THEN
        INSERT IGNORE INTO CaseOpeningUserFreeCaseAllowances(UserId,WindowStartedUtc,QuantityClaimed) VALUES(p_user_id,UTC_TIMESTAMP(6),0);
        SELECT WindowStartedUtc,QuantityClaimed INTO v_window,v_claimed FROM CaseOpeningUserFreeCaseAllowances WHERE UserId=p_user_id FOR UPDATE;
        IF v_window<=UTC_TIMESTAMP(6)-INTERVAL v_allowance_hours HOUR THEN SET v_window=UTC_TIMESTAMP(6); SET v_claimed=0; END IF;
        IF v_claimed+p_quantity>v_allowance_quantity THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The replenishing free-case allowance has been used. Try again when it refreshes.'; END IF;
        UPDATE CaseOpeningUserFreeCaseAllowances SET WindowStartedUtc=v_window,QuantityClaimed=v_claimed+p_quantity WHERE UserId=p_user_id;
    END IF;
    INSERT IGNORE INTO CaseOpeningInventoryCapacity(UserId,BaseCapacity,UpdatedUtc) VALUES(p_user_id,1000,UTC_TIMESTAMP(6)); INSERT IGNORE INTO CaseOpeningUserInventoryUpgrades(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6));
    SELECT BaseCapacity INTO v_base_capacity FROM CaseOpeningInventoryCapacity WHERE UserId=p_user_id FOR UPDATE; SELECT COALESCE(SUM(AddedSlots),0) INTO v_storage_slots FROM CaseOpeningStorageContainers WHERE UserId=p_user_id; SELECT BonusInventorySlots INTO v_upgrade_slots FROM CaseOpeningUserInventoryUpgrades WHERE UserId=p_user_id; SELECT COUNT(*) INTO v_skin_slots FROM CaseOpeningHistory WHERE UserId=p_user_id; SELECT COALESCE(SUM(Quantity),0) INTO v_case_slots FROM CaseOpeningOwnedCases WHERE UserId=p_user_id;
    IF p_quantity>GREATEST(v_base_capacity+v_storage_slots+v_upgrade_slots-v_skin_slots-v_case_slots,0) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough inventory space for these cases.'; END IF;
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_total_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_total_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_total_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency to buy these cases.'; END IF;
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) VALUES(p_user_id,p_case_key,p_quantity,UTC_TIMESTAMP(6)) ON DUPLICATE KEY UPDATE Quantity=Quantity+VALUES(Quantity),UpdatedUtc=UTC_TIMESTAMP(6);
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_total_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'case-purchase','case',p_case_key,NULL,UTC_TIMESTAMP(6));
    COMMIT;
    SELECT p_case_key CaseKey,p_quantity PurchasedQuantity,Quantity OwnedQuantity,IF(v_mode='stars',v_total_cost,0) StarsSpent,(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id) StarsBalance,v_mode EconomyMode,v_total_cost AmountSpentMinor,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)) BalanceMinor FROM CaseOpeningOwnedCases WHERE UserId=p_user_id AND CaseKey=p_case_key;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_inventory_sell//
CREATE PROCEDURE sp_case_opening_inventory_sell(IN p_user_id CHAR(36),IN p_opening_ids JSON,IN p_item_count INT,IN p_stars_awarded INT,IN p_gbp_pence_awarded BIGINT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_award BIGINT; DECLARE v_count INT DEFAULT 0; DECLARE v_deleted INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_award=IF(v_mode='gbp',p_gbp_pence_awarded,p_stars_awarded); START TRANSACTION;
    SELECT COUNT(*) INTO v_count FROM CaseOpeningHistory h INNER JOIN (SELECT DISTINCT OpeningId FROM JSON_TABLE(p_opening_ids,'$[*]' COLUMNS(OpeningId CHAR(36) PATH '$')) s) x ON BINARY x.OpeningId=BINARY h.OpeningId WHERE BINARY h.UserId=BINARY p_user_id AND h.IsLocked=0 FOR UPDATE;
    IF v_count<>p_item_count THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='One or more selected items are protected or unavailable.'; END IF;
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars+v_award,Stars),GbpPence=IF(v_mode='gbp',GbpPence+v_award,GbpPence),UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id;
    DELETE h FROM CaseOpeningHistory h INNER JOIN (SELECT DISTINCT OpeningId FROM JSON_TABLE(p_opening_ids,'$[*]' COLUMNS(OpeningId CHAR(36) PATH '$')) s) x ON BINARY x.OpeningId=BINARY h.OpeningId WHERE BINARY h.UserId=BINARY p_user_id AND h.IsLocked=0; SET v_deleted=ROW_COUNT();
    IF v_deleted<>p_item_count THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The inventory changed before the sale completed.'; END IF;
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,v_award,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'inventory-sale','inventory',NULL,NULL,UTC_TIMESTAMP(6));
    COMMIT;
    SELECT IF(v_mode='stars',v_award,0) StarsAwarded,Stars StarsBalance,v_count SoldItemCount,v_mode EconomyMode,v_award AmountAwardedMinor,IF(v_mode='gbp',GbpPence,Stars) BalanceMinor FROM CaseOpeningProgress WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_storage_container_purchase//
CREATE PROCEDURE sp_case_opening_storage_container_purchase(IN p_user_id CHAR(36),IN p_storage_container_id CHAR(36),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT,IN p_slots INT,IN p_maximum_containers INT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT; DECLARE v_count INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars); START TRANSACTION;
    IF v_cost<0 OR p_slots<1 OR p_maximum_containers<0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The storage configuration is not valid.'; END IF;
    INSERT IGNORE INTO CaseOpeningInventoryCapacity(UserId,BaseCapacity,UpdatedUtc) VALUES(p_user_id,1000,UTC_TIMESTAMP(6)); SELECT COUNT(*) INTO v_count FROM CaseOpeningStorageContainers WHERE UserId=p_user_id;
    IF v_count>=p_maximum_containers THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You already own the maximum number of storage containers.'; END IF;
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency to purchase this storage container.'; END IF;
    INSERT INTO CaseOpeningStorageContainers(StorageContainerId,UserId,AddedSlots,AcquiredUtc) VALUES(p_storage_container_id,p_user_id,p_slots,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'storage-purchase','storage',p_storage_container_id,NULL,UTC_TIMESTAMP(6)); COMMIT;
    SELECT v_count+1 StorageContainerCount,p_slots AddedSlots,(SELECT BaseCapacity FROM CaseOpeningInventoryCapacity WHERE UserId=p_user_id)+(v_count+1)*p_slots TotalCapacity,IF(v_mode='stars',v_cost,0) StarsSpent,(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id) StarsBalance,v_mode EconomyMode,v_cost AmountSpentMinor,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)) BalanceMinor;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_upgrade_definitions_get//
CREATE PROCEDURE sp_case_opening_upgrade_definitions_get(IN p_user_id CHAR(36))
BEGIN SELECT d.UpgradeKey,d.Name,d.Description,d.Category,d.CostStars,d.CostGbpPence,d.RequiredLevel,d.SortOrder,IF(u.UpgradeKey IS NULL,0,1) IsUnlocked FROM CaseOpeningUpgradeDefinitions d LEFT JOIN CaseOpeningUserUpgradeUnlocks u ON u.UserId=p_user_id AND u.UpgradeKey=d.UpgradeKey WHERE d.IsActive=1 ORDER BY d.SortOrder,d.UpgradeKey; END//
DROP PROCEDURE IF EXISTS sp_case_opening_upgrade_settings_get//
CREATE PROCEDURE sp_case_opening_upgrade_settings_get() BEGIN SELECT UpgradeKey,Name,Description,Category,CostStars,CostGbpPence,RequiredLevel,SortOrder,0 IsUnlocked FROM CaseOpeningUpgradeDefinitions ORDER BY SortOrder,UpgradeKey; END//
DROP PROCEDURE IF EXISTS sp_case_opening_upgrade_settings_set//
CREATE PROCEDURE sp_case_opening_upgrade_settings_set(IN p_upgrade_key VARCHAR(50),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT,IN p_required_level INT) BEGIN UPDATE CaseOpeningUpgradeDefinitions SET CostStars=GREATEST(0,p_cost_stars),CostGbpPence=GREATEST(0,p_cost_gbp_pence),RequiredLevel=GREATEST(0,p_required_level) WHERE UpgradeKey=p_upgrade_key; END//
DROP PROCEDURE IF EXISTS sp_case_opening_inventory_upgrade_unlock//
CREATE PROCEDURE sp_case_opening_inventory_upgrade_unlock(IN p_user_id CHAR(36),IN p_upgrade_key VARCHAR(50),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT; DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars); START TRANSACTION;
    INSERT IGNORE INTO CaseOpeningUserInventoryUpgrades(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6));
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency to purchase this upgrade.'; END IF;
    INSERT IGNORE INTO CaseOpeningUserUpgradeUnlocks(UserId,UpgradeKey,UnlockedUtc) VALUES(p_user_id,p_upgrade_key,UTC_TIMESTAMP(6)); IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This upgrade is already unlocked.'; END IF;
    UPDATE CaseOpeningUserInventoryUpgrades SET BulkSellLimit=CASE p_upgrade_key WHEN 'bulk-sell-200' THEN GREATEST(BulkSellLimit,200) WHEN 'bulk-sell-300' THEN GREATEST(BulkSellLimit,300) WHEN 'bulk-sell-400' THEN GREATEST(BulkSellLimit,400) WHEN 'bulk-sell-500' THEN GREATEST(BulkSellLimit,500) ELSE BulkSellLimit END,BonusInventorySlots=CASE p_upgrade_key WHEN 'inventory-slots-250' THEN GREATEST(BonusInventorySlots,250) WHEN 'inventory-slots-500' THEN GREATEST(BonusInventorySlots,750) WHEN 'inventory-slots-1000' THEN GREATEST(BonusInventorySlots,1750) ELSE BonusInventorySlots END,AutoSellCovertUnlocked=IF(p_upgrade_key='auto-sell-covert',1,AutoSellCovertUnlocked),AutoSellClassifiedUnlocked=IF(p_upgrade_key='auto-sell-classified',1,AutoSellClassifiedUnlocked),AutoSellRestrictedUnlocked=IF(p_upgrade_key='auto-sell-restricted',1,AutoSellRestrictedUnlocked),AutoSellMilSpecUnlocked=IF(p_upgrade_key='auto-sell-mil-spec',1,AutoSellMilSpecUnlocked),AutoBuyUnlocked=IF(p_upgrade_key='auto-buy-unlock',1,AutoBuyUnlocked),AutoBuyRuleSlots=CASE p_upgrade_key WHEN 'auto-buy-slots-5' THEN GREATEST(AutoBuyRuleSlots,5) WHEN 'auto-buy-slots-10' THEN GREATEST(AutoBuyRuleSlots,10) ELSE AutoBuyRuleSlots END,TradeUpRecipesUnlocked=IF(p_upgrade_key='trade-up-unlock',1,TradeUpRecipesUnlocked),TradeUpRecipeSlots=IF(p_upgrade_key='trade-up-unlock',GREATEST(TradeUpRecipeSlots,1),TradeUpRecipeSlots),UpdatedUtc=UTC_TIMESTAMP(6) WHERE UserId=p_user_id;
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'inventory-upgrade','upgrade',p_upgrade_key,NULL,UTC_TIMESTAMP(6)); COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_achievements_get//
CREATE PROCEDURE sp_case_opening_achievements_get(IN p_user_id CHAR(36))
BEGIN
    SELECT d.AchievementKey,d.Name,d.Description,d.MetricKey,d.TargetValue,d.RewardStars,d.RewardGbpPence,d.SortOrder,IF(u.UserAchievementId IS NULL,0,1) IsUnlocked,u.UnlockedUtc
    FROM CaseOpeningAchievementDefinitions d LEFT JOIN CaseOpeningUserAchievements u ON u.AchievementKey=d.AchievementKey AND u.UserId=p_user_id WHERE d.IsActive=1 ORDER BY d.SortOrder,d.AchievementKey;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_level_reward_claim//
CREATE PROCEDURE sp_case_opening_level_reward_claim(IN p_user_id CHAR(36),IN p_level INT,IN p_stars_awarded INT,IN p_gbp_pence_awarded BIGINT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_claimed INT DEFAULT 0; DECLARE v_award BIGINT;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_award=IF(v_mode='gbp',p_gbp_pence_awarded,p_stars_awarded); START TRANSACTION;
    INSERT IGNORE INTO CaseOpeningPlayerStats(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6)); INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP(6));
    UPDATE CaseOpeningPlayerStats SET HighestRewardedLevel=p_level,UpdatedUtc=UTC_TIMESTAMP(6) WHERE UserId=p_user_id AND HighestRewardedLevel<p_level; SET v_claimed=ROW_COUNT();
    IF v_claimed=1 THEN UPDATE CaseOpeningProgress SET Stars=Stars+IF(v_mode='stars',v_award,0),GbpPence=GbpPence+IF(v_mode='gbp',v_award,0),UpdatedUtc=UTC_TIMESTAMP(6) WHERE UserId=p_user_id; INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,v_award,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'level-reward','level',p_level,NULL,UTC_TIMESTAMP(6)); END IF;
    COMMIT; SELECT v_claimed;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_achievements_evaluate//
CREATE PROCEDURE sp_case_opening_achievements_evaluate(IN p_user_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_stars BIGINT DEFAULT 0; DECLARE v_gbp BIGINT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; START TRANSACTION;
    INSERT IGNORE INTO CaseOpeningPlayerStats(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6)); SELECT UserId FROM CaseOpeningPlayerStats WHERE UserId=p_user_id FOR UPDATE;
    SELECT COALESCE(SUM(d.RewardStars),0),COALESCE(SUM(d.RewardGbpPence),0) INTO v_stars,v_gbp FROM CaseOpeningAchievementDefinitions d INNER JOIN CaseOpeningPlayerStats s ON s.UserId=p_user_id LEFT JOIN CaseOpeningUserAchievements u ON u.UserId=p_user_id AND u.AchievementKey=d.AchievementKey WHERE d.IsActive=1 AND u.UserAchievementId IS NULL AND CASE d.MetricKey WHEN 'cases-opened' THEN s.TotalCasesOpened WHEN 'skins-obtained' THEN s.TotalSkinsObtained WHEN 'trade-ups-completed' THEN s.TotalTradeUpsCompleted WHEN 'unlocks' THEN s.TotalUnlocks WHEN 'login-days' THEN s.TotalLoginDays WHEN 'login-streak' THEN s.CurrentLoginStreak WHEN 'collections-completed' THEN s.CompletedCollections WHEN 'rarity-sets-completed' THEN s.CompletedRaritySets ELSE 0 END>=d.TargetValue;
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP(6));
    UPDATE CaseOpeningProgress SET Stars=Stars+IF(v_mode='stars',v_stars,0),GbpPence=GbpPence+IF(v_mode='gbp',v_gbp,0),UpdatedUtc=UTC_TIMESTAMP(6) WHERE UserId=p_user_id;
    INSERT IGNORE INTO CaseOpeningUserAchievements(UserAchievementId,UserId,AchievementKey,UnlockedUtc) SELECT UUID(),p_user_id,d.AchievementKey,UTC_TIMESTAMP(6) FROM CaseOpeningAchievementDefinitions d INNER JOIN CaseOpeningPlayerStats s ON s.UserId=p_user_id WHERE d.IsActive=1 AND CASE d.MetricKey WHEN 'cases-opened' THEN s.TotalCasesOpened WHEN 'skins-obtained' THEN s.TotalSkinsObtained WHEN 'trade-ups-completed' THEN s.TotalTradeUpsCompleted WHEN 'unlocks' THEN s.TotalUnlocks WHEN 'login-days' THEN s.TotalLoginDays WHEN 'login-streak' THEN s.CurrentLoginStreak WHEN 'collections-completed' THEN s.CompletedCollections WHEN 'rarity-sets-completed' THEN s.CompletedRaritySets ELSE 0 END>=d.TargetValue;
    IF IF(v_mode='gbp',v_gbp,v_stars)>0 THEN INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,IF(v_mode='gbp',v_gbp,v_stars),IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'achievement-reward','achievement',NULL,NULL,UTC_TIMESTAMP(6)); END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_bot_server_purchase//
CREATE PROCEDURE sp_case_opening_bot_server_purchase(IN p_user_id CHAR(36),IN p_server_id CHAR(36),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT; DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars); START TRANSACTION;
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency to purchase this bot server.'; END IF;
    INSERT INTO CaseOpeningBotServers(ServerId,UserId,CreatedUtc) VALUES(p_server_id,p_user_id,UTC_TIMESTAMP(6)); INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'bot-server-purchase','bot-server',p_server_id,NULL,UTC_TIMESTAMP(6)); COMMIT;
END//
DROP PROCEDURE IF EXISTS sp_case_opening_trade_up_recipe_create//
CREATE PROCEDURE sp_case_opening_trade_up_recipe_create(IN p_user_id CHAR(36),IN p_recipe_id CHAR(36),IN p_target_case_key VARCHAR(80),IN p_target_source_item_id VARCHAR(160),IN p_target_item_name VARCHAR(255),IN p_target_market_hash_name VARCHAR(300),IN p_target_image_url VARCHAR(2048),IN p_target_rarity_key VARCHAR(30),IN p_target_rarity_name VARCHAR(80),IN p_target_rarity_color CHAR(7),IN p_target_input_rarity_key VARCHAR(30),IN p_target_stat_trak TINYINT(1),IN p_target_wears JSON,IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT,IN p_recipe_slot_cap INT UNSIGNED)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT; DECLARE v_count INT DEFAULT 0; DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars); START TRANSACTION;
    SELECT COUNT(*) INTO v_count FROM CaseOpeningTradeUpRecipes WHERE UserId=p_user_id AND IsActive=1 FOR UPDATE; IF v_count>=p_recipe_slot_cap THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You have reached your active recipe limit.'; END IF;
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost; IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency to create this recipe.'; END IF;
    INSERT INTO CaseOpeningTradeUpRecipes(RecipeId,UserId,TargetCaseKey,TargetSourceItemId,TargetItemName,TargetMarketHashName,TargetImageUrl,TargetRarityKey,TargetRarityName,TargetRarityColor,TargetInputRarityKey,TargetStatTrak,TargetWears,IsActive,CreatedUtc,UpdatedUtc) VALUES(p_recipe_id,p_user_id,p_target_case_key,p_target_source_item_id,p_target_item_name,p_target_market_hash_name,p_target_image_url,p_target_rarity_key,p_target_rarity_name,p_target_rarity_color,p_target_input_rarity_key,p_target_stat_trak,p_target_wears,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'trade-up-recipe','recipe',p_recipe_id,NULL,UTC_TIMESTAMP(6)); COMMIT;
END//
DROP PROCEDURE IF EXISTS sp_case_opening_bot_speed_upgrade//
CREATE PROCEDURE sp_case_opening_bot_speed_upgrade(IN p_user_id CHAR(36),IN p_bot_id CHAR(36),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT,IN p_maximum_level INT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT; DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars); START TRANSACTION;
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency for this bot upgrade.'; END IF;
    UPDATE CaseOpeningBots SET SpeedLevel=SpeedLevel+1 WHERE BotId=p_bot_id AND UserId=p_user_id AND SpeedLevel<p_maximum_level; IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This bot is already at maximum speed or could not be found.'; END IF;
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'bot-speed-upgrade','bot',p_bot_id,NULL,UTC_TIMESTAMP(6)); COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_bot_purchase//
CREATE PROCEDURE sp_case_opening_bot_purchase(IN p_user_id CHAR(36),IN p_server_id CHAR(36),IN p_bot_id CHAR(36),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT; DECLARE v_count INT DEFAULT 0; DECLARE v_server CHAR(36); DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars); START TRANSACTION;
    SELECT ServerId INTO v_server FROM CaseOpeningBotServers WHERE ServerId=p_server_id AND UserId=p_user_id FOR UPDATE; IF v_server IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The selected bot server could not be found.'; END IF;
    SELECT COUNT(*) INTO v_count FROM CaseOpeningBots WHERE ServerId=p_server_id AND UserId=p_user_id; IF v_count>=4 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This bot server is full.'; END IF;
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency to purchase this bot.'; END IF;
    INSERT INTO CaseOpeningBots(BotId,ServerId,UserId,CreatedUtc,LastOpenedUtc) VALUES(p_bot_id,p_server_id,p_user_id,UTC_TIMESTAMP(6),NULL); INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'bot-purchase','bot',p_bot_id,NULL,UTC_TIMESTAMP(6)); COMMIT;
END//
DROP PROCEDURE IF EXISTS sp_case_opening_player_stats_get//
CREATE PROCEDURE sp_case_opening_player_stats_get(IN p_user_id CHAR(36))
BEGIN
    INSERT IGNORE INTO CaseOpeningPlayerStats(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6));
    SELECT s.UserId,s.TotalCasesOpened,s.TotalSkinsObtained,s.TotalTradeUpsCompleted,s.TotalUnlocks,s.TotalLoginDays,s.CurrentLoginStreak,s.LongestLoginStreak,s.CompletedCollections,s.CompletedRaritySets,s.HighestRewardedLevel,s.LastLoginUtcDate,s.TotalMilSpecPulls,s.TotalRestrictedPulls,s.TotalClassifiedPulls,s.TotalCovertPulls,s.TotalRareSpecialPulls,s.TotalStatTrakPulls,s.TotalCasesPurchased,s.TotalCasePurchaseStarsSpent,s.TotalSaleStarsEarned,s.TotalPullValueStars,s.TotalStarsSpent,s.TotalLevelRewardStars,s.TotalUpgradesPurchased,
      COALESCE((SELECT -SUM(AmountMinor) FROM CaseOpeningEconomyLedger l WHERE l.UserId=p_user_id AND l.EconomyMode='gbp' AND l.AmountMinor<0),0) TotalGbpPenceSpent,
      COALESCE((SELECT SUM(AmountMinor) FROM CaseOpeningEconomyLedger l WHERE l.UserId=p_user_id AND l.EconomyMode='gbp' AND l.AmountMinor>0),0) TotalGbpPenceEarned,
      COALESCE((SELECT -SUM(AmountMinor) FROM CaseOpeningEconomyLedger l WHERE l.UserId=p_user_id AND l.EconomyMode='gbp' AND l.TransactionType='case-purchase'),0) TotalGbpCasePurchasePenceSpent,
      COALESCE((SELECT SUM(AmountMinor) FROM CaseOpeningEconomyLedger l WHERE l.UserId=p_user_id AND l.EconomyMode='gbp' AND l.TransactionType='inventory-sale'),0) TotalGbpSalePenceEarned,
      COALESCE((SELECT SUM(AmountMinor) FROM CaseOpeningEconomyLedger l WHERE l.UserId=p_user_id AND l.EconomyMode='gbp' AND l.TransactionType='level-reward'),0) TotalGbpLevelRewardPence,
      COALESCE((SELECT SUM(AmountMinor) FROM CaseOpeningEconomyLedger l WHERE l.UserId=p_user_id AND l.EconomyMode='gbp' AND l.TransactionType='achievement-reward'),0) TotalGbpAchievementRewardPence
    FROM CaseOpeningPlayerStats s WHERE s.UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_price_snapshot_active_price_get//
CREATE PROCEDURE sp_case_opening_price_snapshot_active_price_get(IN p_market_hash_name VARCHAR(300))
BEGIN
    SELECT i.PriceSnapshotId,i.MarketHashName,i.Price,i.MinimumPrice,i.MeanPrice,i.MedianPrice,
           i.SuggestedPrice,i.Quantity,0 IsFallback,i.SourceUpdatedUtc
    FROM CaseOpeningPriceSnapshotItems i
    INNER JOIN CaseOpeningPriceSnapshots s ON s.PriceSnapshotId=i.PriceSnapshotId
    WHERE s.IsActive=1 AND BINARY i.MarketHashName=BINARY p_market_hash_name
    LIMIT 1;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_progress_dev_set//
CREATE PROCEDURE sp_case_opening_progress_dev_set(IN p_user_id CHAR(36),IN p_stars INT,IN p_gbp_pence BIGINT,IN p_xp INT)
BEGIN
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc)
    VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
    UPDATE CaseOpeningProgress SET Stars=p_stars,GbpPence=p_gbp_pence,Xp=p_xp,UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id;
    SELECT UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel FROM CaseOpeningProgress WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_free_case_allowance_get//
CREATE PROCEDURE sp_case_opening_free_case_allowance_get(IN p_user_id CHAR(36))
BEGIN
    DECLARE v_enabled TINYINT DEFAULT 0; DECLARE v_quantity INT DEFAULT 0; DECLARE v_hours INT DEFAULT 24;
    DECLARE v_claimed INT DEFAULT 0; DECLARE v_window DATETIME(6) DEFAULT NULL;
    SELECT FreeCaseAllowanceEnabled,FreeCaseAllowanceQuantity,FreeCaseAllowanceHours INTO v_enabled,v_quantity,v_hours FROM CaseOpeningGameSettings WHERE Id=1;
    SELECT WindowStartedUtc,QuantityClaimed INTO v_window,v_claimed FROM CaseOpeningUserFreeCaseAllowances WHERE UserId=p_user_id LIMIT 1;
    IF v_window IS NULL OR v_window<=UTC_TIMESTAMP(6)-INTERVAL v_hours HOUR THEN SET v_claimed=0; SET v_window=UTC_TIMESTAMP(6); END IF;
    SELECT IF(v_enabled=1,GREATEST(v_quantity-v_claimed,0),0) Remaining,v_quantity Quantity,IF(v_enabled=1,DATE_ADD(v_window,INTERVAL v_hours HOUR),NULL) RefreshUtc;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_trade_up_recipe_slot_upgrade//
CREATE PROCEDURE sp_case_opening_trade_up_recipe_slot_upgrade(IN p_user_id CHAR(36),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT,IN p_maximum_slots INT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars);
    START TRANSACTION;
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency for this recipe-slot upgrade.'; END IF;
    UPDATE CaseOpeningUserInventoryUpgrades SET TradeUpRecipeSlots=TradeUpRecipeSlots+1,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE UserId=p_user_id AND TradeUpRecipesUnlocked=1 AND TradeUpRecipeSlots<p_maximum_slots;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Auto trade-up recipe slots are already at maximum or unavailable.'; END IF;
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'trade-up-slot-upgrade','trade-up',NULL,NULL,UTC_TIMESTAMP(6));
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_trade_up_recipe_holding_upgrade//
CREATE PROCEDURE sp_case_opening_trade_up_recipe_holding_upgrade(IN p_user_id CHAR(36),IN p_recipe_id CHAR(36),IN p_cost_stars INT,IN p_cost_gbp_pence BIGINT,IN p_maximum_capacity INT)
BEGIN
    DECLARE v_mode VARCHAR(10); DECLARE v_cost BIGINT;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1; SET v_cost=IF(v_mode='gbp',p_cost_gbp_pence,p_cost_stars);
    START TRANSACTION;
    UPDATE CaseOpeningProgress SET Stars=IF(v_mode='stars',Stars-v_cost,Stars),GbpPence=IF(v_mode='gbp',GbpPence-v_cost,GbpPence),UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id AND IF(v_mode='gbp',GbpPence,Stars)>=v_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency for this holding-capacity upgrade.'; END IF;
    UPDATE CaseOpeningTradeUpRecipes SET HoldingCapacity=HoldingCapacity+1,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE RecipeId=p_recipe_id AND UserId=p_user_id AND HoldingCapacity<p_maximum_capacity;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This recipe''s holding capacity is already at maximum, or the recipe could not be found.'; END IF;
    INSERT INTO CaseOpeningEconomyLedger VALUES(UUID(),p_user_id,v_mode,-v_cost,IF(v_mode='gbp',(SELECT GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id),(SELECT Stars FROM CaseOpeningProgress WHERE UserId=p_user_id)),'trade-up-holding-upgrade','trade-up-recipe',p_recipe_id,NULL,UTC_TIMESTAMP(6));
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_reset_dev//
CREATE PROCEDURE sp_case_opening_reset_dev(IN p_user_id CHAR(36))
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    DELETE FROM CaseOpeningUserAchievements WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningCompletedRarities WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningCompletedCollections WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningPlayerStats WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningBots WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningBotServers WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningTradeUps WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningAutoBuyRules WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningTradeUpRecipeHoldings WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningTradeUpRecipes WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningUserInventoryUpgrades WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningCollection WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningHistory WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningStorageContainers WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningInventoryCapacity WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningOwnedCases WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningUnlockedCases WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningUserFreeCaseAllowances WHERE UserId=p_user_id;
    DELETE FROM CaseOpeningEconomyLedger WHERE UserId=p_user_id;
    INSERT INTO CaseOpeningUnlockedCases(UserId,CaseKey,UnlockedUtc) VALUES(p_user_id,'kilowatt',UTC_TIMESTAMP());
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) VALUES(p_user_id,'kilowatt',25,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningInventoryCapacity(UserId,BaseCapacity,UpdatedUtc) VALUES(p_user_id,1000,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc)
    VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP())
    ON DUPLICATE KEY UPDATE Stars=0,GbpPence=0,Xp=0,SkipAnimationUnlocked=0,MultiOpenLevel=0,OpenSpeedLevel=0,UpdatedUtc=UTC_TIMESTAMP();
    COMMIT;
END//
DELIMITER ;

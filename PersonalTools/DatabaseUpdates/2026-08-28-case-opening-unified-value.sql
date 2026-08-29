-- GBP is the canonical case-opening value, stored in pence. Stars are a whole-pound display:
-- £100.00 is 100 Stars. Run this once, after the dual-economy foundation migration.

UPDATE CaseOpeningProgress SET Stars=ROUND(GbpPence/100);
UPDATE CaseOpeningCaseSettings SET UnlockCostStars=ROUND(UnlockCostGbpPence/100),PurchaseCostStars=ROUND(PurchaseCostGbpPence/100);
UPDATE CaseOpeningUpgradeDefinitions SET CostStars=ROUND(CostGbpPence/100);
UPDATE CaseOpeningAchievementDefinitions SET RewardStars=ROUND(RewardGbpPence/100);

UPDATE CaseOpeningGameSettings SET
    SkipAnimationCostStars=ROUND(SkipAnimationCostGbpPence/100),
    MultiOpenCostStars=ROUND(MultiOpenCostGbpPence/100),
    OpenSpeedUpgradeBaseCostStars=ROUND(OpenSpeedUpgradeBaseCostGbpPence/100),
    OpenSpeedUpgradeCostIncrementStars=ROUND(OpenSpeedUpgradeCostIncrementGbpPence/100),
    BotServerBaseCostStars=ROUND(BotServerBaseCostGbpPence/100),
    BotServerCostIncrementStars=ROUND(BotServerCostIncrementGbpPence/100),
    BotBaseCostStars=ROUND(BotBaseCostGbpPence/100),
    StorageContainerBaseCostStars=ROUND(StorageContainerBaseCostGbpPence/100),
    StorageContainerCostIncrementStars=ROUND(StorageContainerCostIncrementGbpPence/100),
    TradeUpRecipeCostStars=ROUND(TradeUpRecipeCostGbpPence/100),
    TradeUpSlotUpgradeBaseCostStars=ROUND(TradeUpSlotUpgradeBaseCostGbpPence/100),
    TradeUpSlotUpgradeCostIncrementStars=ROUND(TradeUpSlotUpgradeCostIncrementGbpPence/100),
    TradeUpHoldingUpgradeBaseCostStars=ROUND(TradeUpHoldingUpgradeBaseCostGbpPence/100),
    TradeUpHoldingUpgradeCostIncrementStars=ROUND(TradeUpHoldingUpgradeCostIncrementGbpPence/100),
    UpdatedUtc=UTC_TIMESTAMP();

DELIMITER $$
DROP TRIGGER IF EXISTS trg_case_opening_progress_unified_value_insert$$
CREATE TRIGGER trg_case_opening_progress_unified_value_insert
BEFORE INSERT ON CaseOpeningProgress FOR EACH ROW
BEGIN
    IF NEW.GbpPence>0 THEN SET NEW.Stars=ROUND(NEW.GbpPence/100);
    ELSEIF NEW.Stars>0 THEN SET NEW.GbpPence=NEW.Stars*100;
    END IF;
END$$

DROP TRIGGER IF EXISTS trg_case_opening_progress_unified_value_update$$
CREATE TRIGGER trg_case_opening_progress_unified_value_update
BEFORE UPDATE ON CaseOpeningProgress FOR EACH ROW
BEGIN
    IF NEW.GbpPence<>OLD.GbpPence AND NEW.Stars=OLD.Stars THEN
        SET NEW.Stars=ROUND(NEW.GbpPence/100);
    ELSEIF NEW.Stars<>OLD.Stars AND NEW.GbpPence=OLD.GbpPence THEN
        SET NEW.GbpPence=NEW.Stars*100;
    ELSEIF NEW.Stars*100<>NEW.GbpPence THEN
        SET NEW.Stars=ROUND(NEW.GbpPence/100);
    END IF;
END$$
DELIMITER ;

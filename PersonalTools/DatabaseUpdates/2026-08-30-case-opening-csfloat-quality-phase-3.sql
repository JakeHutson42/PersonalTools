-- Phase 3: configurable conversion for CSFloat's USD listing prices and strict price-quality support.
ALTER TABLE CaseOpeningGameSettings
    ADD COLUMN IF NOT EXISTS CsFloatUsdToGbpBasisPoints INT UNSIGNED NOT NULL DEFAULT 7800 AFTER GlobalReturnMultiplierBasisPoints;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_game_settings_get//
CREATE PROCEDURE sp_case_opening_game_settings_get()
BEGIN
    SELECT EconomyMode,SkinSaleRateBasisPoints,GlobalReturnMultiplierBasisPoints,CsFloatUsdToGbpBasisPoints,
           FreeCaseAllowanceEnabled,FreeCaseAllowanceQuantity,FreeCaseAllowanceHours,
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

DROP PROCEDURE IF EXISTS sp_case_opening_csfloat_exchange_rate_set//
CREATE PROCEDURE sp_case_opening_csfloat_exchange_rate_set(IN p_basis_points INT)
BEGIN
    UPDATE CaseOpeningGameSettings
    SET CsFloatUsdToGbpBasisPoints=LEAST(20000,GREATEST(1000,p_basis_points)),UpdatedUtc=UTC_TIMESTAMP()
    WHERE Id=1;
END//
DELIMITER ;

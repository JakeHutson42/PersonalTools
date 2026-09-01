-- Phase 7 rollout guard and final legacy-state reconciliation. Safe to rerun.
-- The tree remains opt-in: applying this migration never enables it.
INSERT INTO CaseOpeningSkillTreeSettings(SettingsId,Enabled,UpdatedUtc) VALUES(1,0,UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE Enabled=0,UpdatedUtc=UTC_TIMESTAMP();

-- Reconcile every legacy upgrade represented by account columns into the generic unlock ledger.
-- Opening speed, multi-open, storage containers, recipe-slot counts and Daily Drop ranks remain
-- sourced directly from their original persisted values and therefore require no ledger copy.
INSERT IGNORE INTO CaseOpeningUserUpgradeUnlocks(UserId,UpgradeKey,UnlockedUtc)
SELECT u.UserId,d.UpgradeKey,UTC_TIMESTAMP(6)
FROM CaseOpeningUserInventoryUpgrades u
JOIN CaseOpeningUpgradeDefinitions d ON
 (d.UpgradeKey='bulk-sell-200' AND u.BulkSellLimit>=200) OR
 (d.UpgradeKey='bulk-sell-300' AND u.BulkSellLimit>=300) OR
 (d.UpgradeKey='bulk-sell-400' AND u.BulkSellLimit>=400) OR
 (d.UpgradeKey='bulk-sell-500' AND u.BulkSellLimit>=500) OR
 (d.UpgradeKey='auto-sell-covert' AND u.AutoSellCovertUnlocked=1) OR
 (d.UpgradeKey='auto-sell-classified' AND u.AutoSellClassifiedUnlocked=1) OR
 (d.UpgradeKey='auto-sell-restricted' AND u.AutoSellRestrictedUnlocked=1) OR
 (d.UpgradeKey='auto-sell-mil-spec' AND u.AutoSellMilSpecUnlocked=1) OR
 (d.UpgradeKey='inventory-slots-250' AND u.BonusInventorySlots>=250) OR
 (d.UpgradeKey='inventory-slots-500' AND u.BonusInventorySlots>=750) OR
 (d.UpgradeKey='inventory-slots-1000' AND u.BonusInventorySlots>=1750) OR
 (d.UpgradeKey='auto-buy-unlock' AND u.AutoBuyUnlocked=1) OR
 (d.UpgradeKey='auto-buy-slots-5' AND u.AutoBuyRuleSlots>=5) OR
 (d.UpgradeKey='auto-buy-slots-10' AND u.AutoBuyRuleSlots>=10) OR
 (d.UpgradeKey='trade-up-unlock' AND u.TradeUpRecipesUnlocked=1);

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_skill_tree_legacy_audit//
CREATE PROCEDURE sp_case_opening_skill_tree_legacy_audit()
BEGIN
 SELECT
  (SELECT COUNT(*) FROM CaseOpeningProgress WHERE OpenSpeedLevel>0 OR MultiOpenLevel>0) AS AccountsWithLegacyOpeningProgress,
  (SELECT COUNT(*) FROM CaseOpeningUserInventoryUpgrades WHERE BulkSellLimit>100 OR BonusInventorySlots>0 OR AutoBuyUnlocked=1 OR TradeUpRecipesUnlocked=1 OR AutoSellCovertUnlocked=1 OR AutoSellClassifiedUnlocked=1 OR AutoSellRestrictedUnlocked=1 OR AutoSellMilSpecUnlocked=1) AS AccountsWithLegacyInventoryProgress,
  (SELECT COUNT(DISTINCT UserId) FROM CaseOpeningUserUpgradeUnlocks) AS AccountsMappedToUnlockLedger,
  (SELECT COUNT(*) FROM CaseOpeningDailyDropUpgrades WHERE Level>0) AS DailyDropRanksPreserved,
  (SELECT Enabled FROM CaseOpeningSkillTreeSettings WHERE SettingsId=1) AS SkillTreeEnabled;
END//
DELIMITER ;

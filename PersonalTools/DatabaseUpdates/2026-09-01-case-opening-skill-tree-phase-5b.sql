-- Phase 5B: inventory and automation quality-of-life progression.
INSERT INTO CaseOpeningUpgradeDefinitions
    (UpgradeKey,Name,Description,Category,CostStars,CostGbpPence,RequiredLevel,SortOrder,IsActive)
VALUES
('inventory-filters','Inventory Filters','Unlock dedicated rarity and wear filters in the inventory.','inventory-qol',1800,180,8,700,1),
('auto-buy-reserve','Auto-Buy Reserve','Prevent automatic case purchases from taking the active balance below a configured reserve.','automation-qol',2800,280,12,710,1),
('auto-buy-case-groups','Auto-Buy Case Groups','Limit automatic restocking to the case currently selected in the opening machine.','automation-qol',3400,340,14,720,1),
('auto-sell-price-floor','Auto-Sell Price Floor','Protect items valued above a configured amount from automatic selling.','automation-qol',3200,320,13,730,1),
('auto-sell-duplicate-copies','Auto-Sell Duplicate Copies','Keep a configured number of identical skins before automatic selling begins.','automation-qol',3800,380,15,740,1),
('auto-sell-wear-filters','Auto-Sell Wear Filters','Restrict automatic selling to selected exterior wear conditions.','automation-qol',4200,420,16,750,1),
('trade-up-reserve','Trade-Up Reserve','Keep a configured number of matching ingredients outside automatic Trade Ups.','trade-up-qol',4200,420,16,760,1),
('pause-automation-storage-low','Pause Automation When Storage Is Low','Pause automatic purchases and Trade Ups when free storage reaches a configured threshold.','automation-qol',5000,500,18,770,1)
ON DUPLICATE KEY UPDATE Name=VALUES(Name),Description=VALUES(Description),Category=VALUES(Category),SortOrder=VALUES(SortOrder),IsActive=VALUES(IsActive);

ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS AutoBuyReserveMinor BIGINT NOT NULL DEFAULT 0;
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS FollowSelectedCase TINYINT(1) NOT NULL DEFAULT 0;
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS SelectedCaseKey VARCHAR(80) NULL;
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS AutoSellProtectAboveMinor BIGINT NOT NULL DEFAULT 0;
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS AutoSellDuplicateCopies INT NOT NULL DEFAULT 0;
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS AutoSellWears VARCHAR(200) NOT NULL DEFAULT '';
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS TradeUpReserve INT NOT NULL DEFAULT 0;
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS PauseAutomationFreeSlots INT NOT NULL DEFAULT 0;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_user_preferences_get//
CREATE PROCEDURE sp_case_opening_user_preferences_get(IN p_user_id CHAR(36))
BEGIN
 INSERT IGNORE INTO CaseOpeningUserPreferences(UserId) VALUES(p_user_id);
 SELECT LastOpenQuantity,AutoBuyReserveMinor,FollowSelectedCase,SelectedCaseKey,AutoSellProtectAboveMinor,AutoSellDuplicateCopies,AutoSellWears,TradeUpReserve,PauseAutomationFreeSlots FROM CaseOpeningUserPreferences WHERE UserId=p_user_id;
END//
DROP PROCEDURE IF EXISTS sp_case_opening_automation_preferences_set//
CREATE PROCEDURE sp_case_opening_automation_preferences_set(IN p_user_id CHAR(36),IN p_auto_buy_reserve BIGINT,IN p_follow_selected TINYINT,IN p_selected_case_key VARCHAR(80),IN p_auto_sell_protect_above BIGINT,IN p_duplicate_copies INT,IN p_wears VARCHAR(200),IN p_trade_up_reserve INT,IN p_pause_free_slots INT)
BEGIN
 IF p_auto_buy_reserve<0 OR p_auto_sell_protect_above<0 OR p_duplicate_copies<0 OR p_duplicate_copies>100 OR p_trade_up_reserve<0 OR p_trade_up_reserve>100 OR p_pause_free_slots<0 OR p_pause_free_slots>10000 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='One or more automation safeguards are outside their allowed range.'; END IF;
 INSERT INTO CaseOpeningUserPreferences(UserId,AutoBuyReserveMinor,FollowSelectedCase,SelectedCaseKey,AutoSellProtectAboveMinor,AutoSellDuplicateCopies,AutoSellWears,TradeUpReserve,PauseAutomationFreeSlots,UpdatedUtc)
 VALUES(p_user_id,p_auto_buy_reserve,p_follow_selected,NULLIF(p_selected_case_key,''),p_auto_sell_protect_above,p_duplicate_copies,p_wears,p_trade_up_reserve,p_pause_free_slots,UTC_TIMESTAMP(6))
 ON DUPLICATE KEY UPDATE AutoBuyReserveMinor=VALUES(AutoBuyReserveMinor),FollowSelectedCase=VALUES(FollowSelectedCase),SelectedCaseKey=VALUES(SelectedCaseKey),AutoSellProtectAboveMinor=VALUES(AutoSellProtectAboveMinor),AutoSellDuplicateCopies=VALUES(AutoSellDuplicateCopies),AutoSellWears=VALUES(AutoSellWears),TradeUpReserve=VALUES(TradeUpReserve),PauseAutomationFreeSlots=VALUES(PauseAutomationFreeSlots),UpdatedUtc=UTC_TIMESTAMP(6);
 CALL sp_case_opening_user_preferences_get(p_user_id);
END//
DELIMITER ;

-- Phase 5D: cosmetic battle/profile presentation. No setting changes odds, value or settlement.
INSERT INTO CaseOpeningUpgradeDefinitions(UpgradeKey,Name,Description,Category,CostStars,CostGbpPence,RequiredLevel,SortOrder,IsActive) VALUES
('reaction-wheel-slots','Additional Reaction Wheel Slots','Expand the battle reaction wheel from four saved reactions to eight.','social-qol',2200,220,10,850,1),
('saved-reaction-layout','Saved Reaction Layout','Choose and save the order of reactions shown in the battle wheel.','social-qol',2800,280,12,860,1),
('victory-emote-slot','Victory Emote Slot','Choose one owned reaction to send automatically after a battle victory.','social-qol',3400,340,14,870,1),
('profile-showcase-slot','Profile Showcase Slot','Display one owned skin in the Case Tycoon profile sidebar.','social-qol',3800,380,15,880,1),
('battle-history-filters','Battle History Filters','Filter profile battle history by result and battle format.','social-qol',2600,260,12,890,1)
ON DUPLICATE KEY UPDATE Name=VALUES(Name),Description=VALUES(Description),Category=VALUES(Category),SortOrder=VALUES(SortOrder),IsActive=VALUES(IsActive);

ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS ReactionLayout VARCHAR(500) NOT NULL DEFAULT '';
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS VictoryEmoteKey VARCHAR(50) NULL;
ALTER TABLE CaseOpeningUserPreferences ADD COLUMN IF NOT EXISTS ProfileShowcaseOpeningId CHAR(36) NULL;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_user_preferences_get//
CREATE PROCEDURE sp_case_opening_user_preferences_get(IN p_user_id CHAR(36))
BEGIN
 INSERT IGNORE INTO CaseOpeningUserPreferences(UserId) VALUES(p_user_id);
 SELECT LastOpenQuantity,AutoBuyReserveMinor,FollowSelectedCase,SelectedCaseKey,AutoSellProtectAboveMinor,AutoSellDuplicateCopies,AutoSellWears,TradeUpReserve,PauseAutomationFreeSlots,ReactionLayout,VictoryEmoteKey,ProfileShowcaseOpeningId FROM CaseOpeningUserPreferences WHERE UserId=p_user_id;
END//
DROP PROCEDURE IF EXISTS sp_case_opening_social_preferences_set//
CREATE PROCEDURE sp_case_opening_social_preferences_set(IN p_user_id CHAR(36),IN p_layout VARCHAR(500),IN p_victory_emote VARCHAR(50),IN p_showcase_id CHAR(36))
BEGIN
 INSERT INTO CaseOpeningUserPreferences(UserId,ReactionLayout,VictoryEmoteKey,ProfileShowcaseOpeningId,UpdatedUtc) VALUES(p_user_id,p_layout,NULLIF(p_victory_emote,''),NULLIF(p_showcase_id,''),UTC_TIMESTAMP(6))
 ON DUPLICATE KEY UPDATE ReactionLayout=VALUES(ReactionLayout),VictoryEmoteKey=VALUES(VictoryEmoteKey),ProfileShowcaseOpeningId=VALUES(ProfileShowcaseOpeningId),UpdatedUtc=UTC_TIMESTAMP(6);
 CALL sp_case_opening_user_preferences_get(p_user_id);
END//
DELIMITER ;

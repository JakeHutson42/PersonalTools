-- Phase 6: deliberately expensive reward expansion and endgame convergence.
INSERT INTO CaseOpeningUpgradeDefinitions(UpgradeKey,Name,Description,Category,CostStars,CostGbpPence,RequiredLevel,SortOrder,IsActive) VALUES
('reward-preview','Reward Preview','Reveal today''s exact Daily Drop offer after earning the first point of Daily Drop XP.','reward-endgame',6000,600,18,900,1),
('flexible-daily-choice','Flexible Daily Drop Choice','Add a fifth, distinct case-pack option to each Daily Drop offer. You still claim two rewards.','reward-endgame',8500,850,21,910,1),
('streak-insurance','Streak Insurance','Preserve your login streak through one missed UTC day. It does not grant the missed day or its rewards.','reward-endgame',11000,1100,24,920,1),
('bonus-daily-choice','Bonus Daily Drop Choice','Claim three rewards from each completed Daily Drop instead of two.','reward-endgame',16000,1600,28,930,1),
('case-variety','Case Variety','Add a sixth offer drawn from a different unlocked case whenever the catalogue allows it.','reward-endgame',13500,1350,26,940,1),
('collector-convergence','Collector Convergence','Converge completed Daily Drop, social and collection-facing progression into the endgame path.','tree-convergence',22000,2200,32,950,1),
('automation-convergence','Automation Convergence','Converge the completed opening, inventory and automation paths into the endgame path.','tree-convergence',26000,2600,34,960,1),
('reward-convergence','Reward Convergence','Converge every Phase 6 reward extension and unlock final mastery.','tree-convergence',30000,3000,36,970,1),
('tycoon-mastery-1','Tycoon Mastery I','Earn 1 additional Daily Drop XP from each eligible manual case opening.','endgame-mastery',45000,4500,40,980,1),
('tycoon-mastery-2','Tycoon Mastery II','Earn a further 1 Daily Drop XP from each eligible manual case opening.','endgame-mastery',70000,7000,45,990,1),
('tycoon-mastery-3','Tycoon Mastery III','Earn a final 1 Daily Drop XP from each eligible manual case opening and complete the tree.','endgame-mastery',100000,10000,50,1000,1)
ON DUPLICATE KEY UPDATE Name=VALUES(Name),Description=VALUES(Description),Category=VALUES(Category),SortOrder=VALUES(SortOrder),IsActive=VALUES(IsActive);

-- One missed UTC date may retain, but never increment, a streak after Streak Insurance is owned.
DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_login_record//
CREATE PROCEDURE sp_case_opening_login_record(IN p_user_id CHAR(36))
BEGIN
    DECLARE v_today DATE DEFAULT UTC_DATE();
    DECLARE v_last_login_date DATE;
    DECLARE v_insured TINYINT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    INSERT IGNORE INTO CaseOpeningPlayerStats(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6));
    SELECT LastLoginUtcDate INTO v_last_login_date FROM CaseOpeningPlayerStats WHERE UserId=p_user_id FOR UPDATE;
    SELECT EXISTS(SELECT 1 FROM CaseOpeningUserUpgradeUnlocks WHERE UserId=p_user_id AND UpgradeKey='streak-insurance') INTO v_insured;
    IF v_last_login_date IS NULL OR v_last_login_date < v_today THEN
        UPDATE CaseOpeningPlayerStats SET
            TotalLoginDays=TotalLoginDays+1,
            CurrentLoginStreak=CASE
                WHEN v_last_login_date=DATE_SUB(v_today,INTERVAL 1 DAY) THEN CurrentLoginStreak+1
                WHEN v_insured=1 AND v_last_login_date=DATE_SUB(v_today,INTERVAL 2 DAY) THEN CurrentLoginStreak
                ELSE 1 END,
            LongestLoginStreak=GREATEST(LongestLoginStreak,CASE
                WHEN v_last_login_date=DATE_SUB(v_today,INTERVAL 1 DAY) THEN CurrentLoginStreak+1
                WHEN v_insured=1 AND v_last_login_date=DATE_SUB(v_today,INTERVAL 2 DAY) THEN CurrentLoginStreak
                ELSE 1 END),
            LastLoginUtcDate=v_today,UpdatedUtc=UTC_TIMESTAMP(6)
        WHERE UserId=p_user_id;
    END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_claim//
CREATE PROCEDURE sp_case_opening_daily_drop_claim(IN p_user_id CHAR(36),IN p_reward_keys JSON,IN p_economy_mode VARCHAR(10))
BEGIN
 DECLARE v_required INT DEFAULT 2; DECLARE v_selected INT DEFAULT 0; DECLARE v_valid INT DEFAULT 0; DECLARE v_skins INT DEFAULT 0; DECLARE v_slots INT DEFAULT 0;
 DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
 SELECT IF(EXISTS(SELECT 1 FROM CaseOpeningUserUpgradeUnlocks WHERE UserId=p_user_id AND UpgradeKey='bonus-daily-choice'),3,2) INTO v_required;
 SELECT COUNT(*) INTO v_selected FROM JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) chosen;
 IF v_selected<>v_required OR JSON_LENGTH(p_reward_keys)<>v_required THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose the required number of Daily Drop rewards.'; END IF;
 START TRANSACTION;
 SELECT COUNT(*) INTO v_valid FROM CaseOpeningDailyDrops d JOIN JSON_TABLE(d.OfferJson,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$.RewardKey')) offered JOIN JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) chosen ON chosen.RewardKey=offered.RewardKey WHERE d.UserId=p_user_id AND d.DropDate=UTC_DATE() AND d.IsCompleted=1 AND d.IsClaimed=0;
 IF v_valid<>v_required THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This Daily Drop offer is no longer available.'; END IF;
 SELECT COUNT(*) INTO v_skins FROM CaseOpeningDailyDrops d JOIN JSON_TABLE(d.OfferJson,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$.RewardKey',Kind VARCHAR(20) PATH '$.Kind')) offered JOIN JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) chosen ON chosen.RewardKey=offered.RewardKey WHERE d.UserId=p_user_id AND offered.Kind='skin';
 SELECT GREATEST(c.BaseCapacity+u.BonusInventorySlots-(SELECT COUNT(*) FROM CaseOpeningHistory WHERE UserId=p_user_id)-(SELECT COALESCE(SUM(Quantity),0) FROM CaseOpeningOwnedCases WHERE UserId=p_user_id),0) INTO v_slots FROM CaseOpeningInventoryCapacity c CROSS JOIN CaseOpeningUserInventoryUpgrades u WHERE u.UserId=p_user_id LIMIT 1;
 IF v_skins>v_slots THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Make room in your inventory before claiming the selected skin.'; END IF;
 INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
 UPDATE CaseOpeningProgress p JOIN (SELECT COALESCE(SUM(IF(o.Kind='money',o.AmountMinor,0)),0) Amount FROM CaseOpeningDailyDrops d JOIN JSON_TABLE(d.OfferJson,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$.RewardKey',Kind VARCHAR(20) PATH '$.Kind',AmountMinor BIGINT PATH '$.AmountMinor')) o JOIN JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) chosen ON chosen.RewardKey=o.RewardKey WHERE d.UserId=p_user_id) reward ON 1=1 SET p.Stars=p.Stars+IF(p_economy_mode='gbp',0,reward.Amount),p.GbpPence=p.GbpPence+IF(p_economy_mode='gbp',reward.Amount,0),p.UpdatedUtc=UTC_TIMESTAMP() WHERE p.UserId=p_user_id;
 INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) SELECT p_user_id,o.CaseKey,o.AmountMinor,UTC_TIMESTAMP() FROM CaseOpeningDailyDrops d JOIN JSON_TABLE(d.OfferJson,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$.RewardKey',Kind VARCHAR(20) PATH '$.Kind',AmountMinor INT PATH '$.AmountMinor',CaseKey VARCHAR(80) PATH '$.CaseKey')) o JOIN JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) chosen ON chosen.RewardKey=o.RewardKey WHERE d.UserId=p_user_id AND o.Kind='cases' ON DUPLICATE KEY UPDATE Quantity=Quantity+VALUES(Quantity),UpdatedUtc=UTC_TIMESTAMP();
 UPDATE CaseOpeningDailyDrops SET IsClaimed=1,UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND DropDate=UTC_DATE() AND IsCompleted=1 AND IsClaimed=0;
 IF ROW_COUNT()<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This Daily Drop has already been claimed.'; END IF;
 COMMIT;
END//
DELIMITER ;

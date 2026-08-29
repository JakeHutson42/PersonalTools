-- Daily Drop offers are serialized by the application with Pascal-case property names.
-- Recreate the claim procedure so existing offers and all future offers are validated correctly.
USE PersonalTools;

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_claim//
CREATE PROCEDURE sp_case_opening_daily_drop_claim(IN p_user_id CHAR(36), IN p_reward_keys JSON, IN p_economy_mode VARCHAR(10))
BEGIN
    DECLARE v_selected_count INT DEFAULT 0;
    DECLARE v_valid_count INT DEFAULT 0;
    DECLARE v_skin_count INT DEFAULT 0;
    DECLARE v_available_slots INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT COUNT(*) INTO v_selected_count FROM JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) selected;
    IF v_selected_count <> 2 OR JSON_LENGTH(p_reward_keys) <> 2 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose exactly two Daily Drop rewards.'; END IF;
    SELECT COUNT(*) INTO v_valid_count
    FROM CaseOpeningDailyDrops d
    JOIN JSON_TABLE(d.OfferJson,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$.RewardKey')) offered
    JOIN JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) selected ON selected.RewardKey=offered.RewardKey
    WHERE d.UserId=p_user_id AND d.DropDate=UTC_DATE() AND d.IsCompleted=1 AND d.IsClaimed=0;
    IF v_valid_count <> 2 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This Daily Drop offer is no longer available.'; END IF;
    SELECT COUNT(*) INTO v_skin_count
    FROM CaseOpeningDailyDrops d JOIN JSON_TABLE(d.OfferJson,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$.RewardKey', Kind VARCHAR(20) PATH '$.Kind')) offered
    JOIN JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) selected ON selected.RewardKey=offered.RewardKey
    WHERE d.UserId=p_user_id AND offered.Kind='skin';
    SELECT GREATEST(c.BaseCapacity+u.BonusInventorySlots-(SELECT COUNT(*) FROM CaseOpeningHistory WHERE UserId=p_user_id)-(SELECT COALESCE(SUM(Quantity),0) FROM CaseOpeningOwnedCases WHERE UserId=p_user_id),0)
    INTO v_available_slots FROM CaseOpeningInventoryCapacity c CROSS JOIN CaseOpeningUserInventoryUpgrades u WHERE u.UserId=p_user_id LIMIT 1;
    IF v_skin_count > v_available_slots THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Make room in your inventory before claiming the selected skin.'; END IF;
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
    UPDATE CaseOpeningProgress p JOIN (
        SELECT COALESCE(SUM(IF(o.Kind='money',o.AmountMinor,0)),0) Amount
        FROM CaseOpeningDailyDrops d JOIN JSON_TABLE(d.OfferJson,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$.RewardKey', Kind VARCHAR(20) PATH '$.Kind', AmountMinor BIGINT PATH '$.AmountMinor')) o
        JOIN JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) s ON s.RewardKey=o.RewardKey WHERE d.UserId=p_user_id
    ) reward ON 1=1 SET p.Stars=p.Stars+IF(p_economy_mode='gbp',0,reward.Amount), p.GbpPence=p.GbpPence+IF(p_economy_mode='gbp',reward.Amount,0), p.UpdatedUtc=UTC_TIMESTAMP() WHERE p.UserId=p_user_id;
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
    SELECT p_user_id,o.CaseKey,o.AmountMinor,UTC_TIMESTAMP() FROM CaseOpeningDailyDrops d JOIN JSON_TABLE(d.OfferJson,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$.RewardKey', Kind VARCHAR(20) PATH '$.Kind', AmountMinor INT PATH '$.AmountMinor', CaseKey VARCHAR(80) PATH '$.CaseKey')) o
    JOIN JSON_TABLE(p_reward_keys,'$[*]' COLUMNS(RewardKey VARCHAR(50) PATH '$')) s ON s.RewardKey=o.RewardKey WHERE d.UserId=p_user_id AND o.Kind='cases'
    ON DUPLICATE KEY UPDATE Quantity=Quantity+VALUES(Quantity),UpdatedUtc=UTC_TIMESTAMP();
    UPDATE CaseOpeningDailyDrops SET IsClaimed=1,UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id;
    COMMIT;
    SELECT Stars,GbpPence FROM CaseOpeningProgress WHERE UserId=p_user_id;
END//

DELIMITER ;

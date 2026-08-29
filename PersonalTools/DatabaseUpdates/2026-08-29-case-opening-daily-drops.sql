-- Daily Drop persistent progression, offers, and upgrades.
-- The UTC date deliberately defines a GMT day.  Every read/claim procedure clears a stale
-- offer so a browser left open across midnight cannot claim yesterday's drop.
USE PersonalTools;

CREATE TABLE IF NOT EXISTS CaseOpeningDailyDrops
(
    UserId CHAR(36) NOT NULL,
    DropDate DATE NOT NULL,
    Xp INT NOT NULL DEFAULT 0,
    IsCompleted TINYINT(1) NOT NULL DEFAULT 0,
    IsClaimed TINYINT(1) NOT NULL DEFAULT 0,
    OfferJson JSON NULL,
    CreatedUtc DATETIME NOT NULL,
    UpdatedUtc DATETIME NOT NULL,
    PRIMARY KEY (UserId),
    CONSTRAINT FK_CaseOpeningDailyDrops_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) ENGINE=InnoDB COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS CaseOpeningDailyDropUpgrades
(
    UserId CHAR(36) NOT NULL,
    UpgradeKey VARCHAR(50) NOT NULL,
    Level TINYINT UNSIGNED NOT NULL DEFAULT 0,
    UpdatedUtc DATETIME NOT NULL,
    PRIMARY KEY (UserId, UpgradeKey),
    CONSTRAINT FK_CaseOpeningDailyDropUpgrades_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) ENGINE=InnoDB COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS CaseOpeningDailyDropSettings (Id TINYINT NOT NULL PRIMARY KEY, RequiredXp INT NOT NULL DEFAULT 100, UpdatedUtc DATETIME NOT NULL) ENGINE=InnoDB;
INSERT IGNORE INTO CaseOpeningDailyDropSettings(Id,RequiredXp,UpdatedUtc) VALUES(1,100,UTC_TIMESTAMP());

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_get//
CREATE PROCEDURE sp_case_opening_daily_drop_get(IN p_user_id CHAR(36))
BEGIN
    INSERT INTO CaseOpeningDailyDrops(UserId,DropDate,Xp,IsCompleted,IsClaimed,OfferJson,CreatedUtc,UpdatedUtc)
    VALUES(p_user_id,UTC_DATE(),0,0,0,NULL,UTC_TIMESTAMP(),UTC_TIMESTAMP())
    ON DUPLICATE KEY UPDATE
        Xp=IF(DropDate<>UTC_DATE(),0,Xp),
        IsCompleted=IF(DropDate<>UTC_DATE(),0,IsCompleted),
        IsClaimed=IF(DropDate<>UTC_DATE(),0,IsClaimed),
        OfferJson=IF(DropDate<>UTC_DATE(),NULL,OfferJson),
        DropDate=UTC_DATE(), UpdatedUtc=UTC_TIMESTAMP();
    SELECT DropDate,Xp,IsCompleted,IsClaimed,OfferJson FROM CaseOpeningDailyDrops WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_settings_get//
CREATE PROCEDURE sp_case_opening_daily_drop_settings_get() BEGIN SELECT RequiredXp FROM CaseOpeningDailyDropSettings WHERE Id=1; END//
DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_settings_set//
CREATE PROCEDURE sp_case_opening_daily_drop_settings_set(IN p_required_xp INT) BEGIN UPDATE CaseOpeningDailyDropSettings SET RequiredXp=p_required_xp,UpdatedUtc=UTC_TIMESTAMP() WHERE Id=1; END//
DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_reset//
CREATE PROCEDURE sp_case_opening_daily_drop_reset(IN p_user_id CHAR(36)) BEGIN INSERT INTO CaseOpeningDailyDrops(UserId,DropDate,Xp,IsCompleted,IsClaimed,OfferJson,CreatedUtc,UpdatedUtc) VALUES(p_user_id,UTC_DATE(),0,0,0,NULL,UTC_TIMESTAMP(),UTC_TIMESTAMP()) ON DUPLICATE KEY UPDATE DropDate=UTC_DATE(),Xp=0,IsCompleted=0,IsClaimed=0,OfferJson=NULL,UpdatedUtc=UTC_TIMESTAMP(); END//

DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_xp_add//
CREATE PROCEDURE sp_case_opening_daily_drop_xp_add(IN p_user_id CHAR(36), IN p_xp_delta INT, IN p_required_xp INT)
BEGIN
    CALL sp_case_opening_daily_drop_get(p_user_id);
    UPDATE CaseOpeningDailyDrops
    SET Xp=LEAST(p_required_xp, Xp+GREATEST(0,p_xp_delta)),
        IsCompleted=IF(LEAST(p_required_xp, Xp+GREATEST(0,p_xp_delta))>=p_required_xp,1,IsCompleted),
        UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id AND IsClaimed=0;
    SELECT DropDate,Xp,IsCompleted,IsClaimed,OfferJson FROM CaseOpeningDailyDrops WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_offer_set//
CREATE PROCEDURE sp_case_opening_daily_drop_offer_set(IN p_user_id CHAR(36), IN p_offer JSON)
BEGIN
    UPDATE CaseOpeningDailyDrops SET OfferJson=p_offer,UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id AND DropDate=UTC_DATE() AND IsCompleted=1 AND IsClaimed=0 AND OfferJson IS NULL;
    SELECT DropDate,Xp,IsCompleted,IsClaimed,OfferJson FROM CaseOpeningDailyDrops WHERE UserId=p_user_id;
END//

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

DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_upgrades_get//
CREATE PROCEDURE sp_case_opening_daily_drop_upgrades_get(IN p_user_id CHAR(36))
BEGIN
    SELECT UpgradeKey,Level FROM CaseOpeningDailyDropUpgrades WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_daily_drop_upgrade_unlock//
CREATE PROCEDURE sp_case_opening_daily_drop_upgrade_unlock(IN p_user_id CHAR(36), IN p_upgrade_key VARCHAR(50), IN p_cost_stars INT, IN p_cost_gbp_pence BIGINT, IN p_economy_mode VARCHAR(10))
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
    UPDATE CaseOpeningProgress SET Stars=Stars-IF(p_economy_mode='gbp',0,p_cost_stars),GbpPence=GbpPence-IF(p_economy_mode='gbp',p_cost_gbp_pence,0),UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id AND Stars>=IF(p_economy_mode='gbp',0,p_cost_stars) AND GbpPence>=IF(p_economy_mode='gbp',p_cost_gbp_pence,0);
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency for this Daily Drop upgrade.'; END IF;
    INSERT INTO CaseOpeningDailyDropUpgrades(UserId,UpgradeKey,Level,UpdatedUtc) VALUES(p_user_id,p_upgrade_key,1,UTC_TIMESTAMP()) ON DUPLICATE KEY UPDATE Level=Level+1,UpdatedUtc=UTC_TIMESTAMP();
    COMMIT;
END//

DELIMITER ;

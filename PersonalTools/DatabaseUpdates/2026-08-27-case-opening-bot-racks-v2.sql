-- V2: compatible bot-rack migration. This file intentionally uses no window functions.
ALTER TABLE CaseOpeningBotServers
    ADD COLUMN IF NOT EXISTS IsEnabled TINYINT(1) NOT NULL DEFAULT 1 AFTER SpeedLevel;

ALTER TABLE CaseOpeningBots
    ADD COLUMN IF NOT EXISTS SpeedLevel TINYINT UNSIGNED NOT NULL DEFAULT 0 AFTER LastOpenedUtc;

-- Preserve legacy server levels. Count earlier bots to obtain a stable zero-based slot number.
-- All arithmetic is explicitly signed before subtracting, avoiding MariaDB unsigned underflow.
UPDATE CaseOpeningBots target
INNER JOIN (
    SELECT b.BotId,
           LEAST(5,GREATEST(0,
               CAST(s.SpeedLevel AS SIGNED)-(CAST((
                   SELECT COUNT(*) FROM CaseOpeningBots earlier
                   WHERE earlier.ServerId=b.ServerId
                     AND (earlier.CreatedUtc<b.CreatedUtc OR (earlier.CreatedUtc=b.CreatedUtc AND earlier.BotId<b.BotId))
               ) AS SIGNED)*5)
           )) AS MigratedLevel
    FROM CaseOpeningBots b
    INNER JOIN CaseOpeningBotServers s ON s.ServerId=b.ServerId
) migration ON migration.BotId=target.BotId
SET target.SpeedLevel=GREATEST(target.SpeedLevel,migration.MigratedLevel);

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_bot_servers_get//
CREATE PROCEDURE sp_case_opening_bot_servers_get(IN p_user_id CHAR(36))
BEGIN
    SELECT s.ServerId,s.UserId,COALESCE(SUM(b.SpeedLevel),0) AS SpeedLevel,s.IsEnabled,s.CreatedUtc
    FROM CaseOpeningBotServers s
    LEFT JOIN CaseOpeningBots b ON b.ServerId=s.ServerId AND b.UserId=s.UserId
    WHERE s.UserId=p_user_id
    GROUP BY s.ServerId,s.UserId,s.IsEnabled,s.CreatedUtc
    ORDER BY s.CreatedUtc,s.ServerId;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_bots_get//
CREATE PROCEDURE sp_case_opening_bots_get(IN p_user_id CHAR(36))
BEGIN
    SELECT BotId,ServerId,UserId,CreatedUtc,LastOpenedUtc,SpeedLevel
    FROM CaseOpeningBots WHERE UserId=p_user_id ORDER BY CreatedUtc,BotId;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_bot_server_enabled_set//
CREATE PROCEDURE sp_case_opening_bot_server_enabled_set(IN p_user_id CHAR(36),IN p_server_id CHAR(36),IN p_is_enabled TINYINT)
BEGIN
    UPDATE CaseOpeningBotServers SET IsEnabled=IF(p_is_enabled<>0,1,0)
    WHERE ServerId=p_server_id AND UserId=p_user_id;
    IF ROW_COUNT()=0 AND NOT EXISTS(SELECT 1 FROM CaseOpeningBotServers WHERE ServerId=p_server_id AND UserId=p_user_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The selected bot server could not be found.';
    END IF;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_bot_speed_upgrade//
CREATE PROCEDURE sp_case_opening_bot_speed_upgrade(IN p_user_id CHAR(36),IN p_bot_id CHAR(36),IN p_cost INT,IN p_maximum_level INT)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    UPDATE CaseOpeningBots SET SpeedLevel=SpeedLevel+1
    WHERE BotId=p_bot_id AND UserId=p_user_id AND SpeedLevel<p_maximum_level;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This bot is already at maximum speed or could not be found.'; END IF;
    UPDATE CaseOpeningProgress SET Stars=Stars-p_cost,UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id AND Stars>=p_cost;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There are not enough Stars for this bot upgrade.'; END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_bot_cycle_claim//
CREATE PROCEDURE sp_case_opening_bot_cycle_claim(IN p_user_id CHAR(36),IN p_bot_id CHAR(36))
BEGIN
    DECLARE v_interval INT DEFAULT 12;
    DECLARE v_level INT DEFAULT 0;
    DECLARE v_enabled TINYINT DEFAULT 0;
    DECLARE v_effective INT DEFAULT 12;
    SELECT g.BotOpeningIntervalSeconds,b.SpeedLevel,s.IsEnabled INTO v_interval,v_level,v_enabled
    FROM CaseOpeningBots b
    INNER JOIN CaseOpeningBotServers s ON s.ServerId=b.ServerId AND s.UserId=b.UserId
    CROSS JOIN CaseOpeningGameSettings g
    WHERE g.Id=1 AND b.BotId=p_bot_id AND b.UserId=p_user_id;
    SET v_effective=GREATEST(1,CEILING(v_interval*(0.5/(0.5+(LEAST(v_level,5)*0.1)))));
    UPDATE CaseOpeningBots SET LastOpenedUtc=UTC_TIMESTAMP(6)
    WHERE BotId=p_bot_id AND UserId=p_user_id AND v_enabled=1
      AND (LastOpenedUtc IS NULL OR LastOpenedUtc<=DATE_SUB(UTC_TIMESTAMP(6),INTERVAL GREATEST(1,v_effective-1) SECOND));
    SELECT ROW_COUNT();
END//
DELIMITER ;

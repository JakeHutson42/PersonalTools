-- Keeps the SQL enforcement aligned with the configurable battle limit and a usable invite window.
USE PersonalTools;
DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_create//
CREATE PROCEDURE sp_case_battles_create(IN p_battle_id CHAR(36), IN p_user_id CHAR(36), IN p_mode VARCHAR(16), IN p_case_keys JSON)
BEGIN
    DECLARE v_case_count INT DEFAULT 0; DECLARE v_max_cases INT DEFAULT 20; DECLARE v_distinct_cases INT DEFAULT 0;
    DECLARE v_required_players INT DEFAULT 0; DECLARE v_locked_rows INT DEFAULT 0; DECLARE v_reservation_slots INT DEFAULT 0;
    DECLARE v_allowance INT DEFAULT 0; DECLARE v_occupied INT DEFAULT 0; DECLARE v_reserved INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT MaxCasesPerBattle INTO v_max_cases FROM CaseOpeningBattleBotSettings WHERE SettingsId=1;
    SET v_max_cases=LEAST(GREATEST(COALESCE(v_max_cases,20),1),50);
    SET v_required_players=CASE p_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 WHEN 'ffa-4' THEN 4 WHEN 'teams-2v2' THEN 4 ELSE 0 END;
    SET v_case_count=COALESCE(JSON_LENGTH(p_case_keys),0);
    IF v_required_players=0 OR v_case_count<1 OR v_case_count>v_max_cases THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose a valid battle mode and a permitted number of cases.'; END IF;
    SELECT COUNT(*) INTO v_distinct_cases FROM (SELECT CaseKey FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) required GROUP BY CaseKey) cases;
    SELECT COUNT(*) INTO v_locked_rows FROM CaseOpeningOwnedCases owned INNER JOIN (SELECT CaseKey,COUNT(*) Quantity FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) required GROUP BY CaseKey) required ON required.CaseKey=owned.CaseKey WHERE owned.UserId=p_user_id AND owned.Quantity>=required.Quantity FOR UPDATE;
    IF v_locked_rows<>v_distinct_cases THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You need to own every selected case before creating a battle.'; END IF;
    INSERT INTO CaseOpeningBattleOverflow(UserId,SlotAllowance,OccupiedSlots,UpdatedUtc) VALUES(p_user_id,100,0,UTC_TIMESTAMP(6)) ON DUPLICATE KEY UPDATE UpdatedUtc=UpdatedUtc;
    SELECT SlotAllowance,OccupiedSlots INTO v_allowance,v_occupied FROM CaseOpeningBattleOverflow WHERE UserId=p_user_id FOR UPDATE;
    SELECT COALESCE(SUM(ReservedSlots),0) INTO v_reserved FROM CaseOpeningBattleOverflowReservations WHERE UserId=p_user_id AND ReleasedUtc IS NULL FOR UPDATE;
    SET v_reservation_slots=v_case_count*v_required_players;
    IF v_occupied+v_reserved+v_reservation_slots>v_allowance THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Your Battle Overflow allowance does not have enough space for this battle.'; END IF;
    UPDATE CaseOpeningOwnedCases owned INNER JOIN (SELECT CaseKey,COUNT(*) Quantity FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) required GROUP BY CaseKey) required ON required.CaseKey=owned.CaseKey SET owned.Quantity=owned.Quantity-required.Quantity,owned.UpdatedUtc=UTC_TIMESTAMP(6) WHERE owned.UserId=p_user_id AND owned.Quantity>=required.Quantity;
    IF ROW_COUNT()<>v_distinct_cases THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Your case ownership changed before the battle could be created.'; END IF;
    INSERT INTO CaseOpeningBattles(BattleId,CreatorUserId,Mode,Status,CreatedUtc,ExpiresUtc) VALUES(p_battle_id,p_user_id,p_mode,'waiting',UTC_TIMESTAMP(6),DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE));
    INSERT INTO CaseOpeningBattleCases(BattleId,RoundNumber,CaseKey) SELECT p_battle_id,ord-1,CaseKey FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(ord FOR ORDINALITY,CaseKey VARCHAR(80) PATH '$')) required;
    INSERT INTO CaseOpeningBattleParticipants(BattleId,UserId,Seat,Team,OverflowReservedSlots,JoinedUtc) VALUES(p_battle_id,p_user_id,1,IF(p_mode='teams-2v2',1,0),v_reservation_slots,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningBattleCaseReservations(BattleId,UserId,CaseKey,Quantity,ReservedUtc) SELECT p_battle_id,p_user_id,CaseKey,COUNT(*),UTC_TIMESTAMP(6) FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) required GROUP BY CaseKey;
    INSERT INTO CaseOpeningBattleOverflowReservations(BattleId,UserId,ReservedSlots,ReservedUtc) VALUES(p_battle_id,p_user_id,v_reservation_slots,UTC_TIMESTAMP(6));
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invite_set//
CREATE PROCEDURE sp_case_battles_invite_set(IN p_battle_id CHAR(36),IN p_creator_user_id CHAR(36),IN p_invited_user_id CHAR(36))
BEGIN
    DECLARE v_creator_count INT DEFAULT 0; DECLARE v_invited_count INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM Users WHERE UserId=p_invited_user_id AND IsActive=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user is no longer available to invite.'; END IF;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_creator_count FROM CaseOpeningBattles battle LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId WHERE (battle.Status='opening' OR (battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6))) AND (battle.CreatorUserId=p_creator_user_id OR battle.InvitedUserId=p_creator_user_id OR participant.UserId=p_creator_user_id);
    IF v_creator_count>5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You can have up to five unresolved case battles at once.'; END IF;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_invited_count FROM CaseOpeningBattles battle LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId WHERE (battle.Status='opening' OR (battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6))) AND (battle.CreatorUserId=p_invited_user_id OR battle.InvitedUserId=p_invited_user_id OR participant.UserId=p_invited_user_id);
    IF v_invited_count>=5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user already has five unresolved case battles.'; END IF;
    UPDATE CaseOpeningBattles SET InvitedUserId=p_invited_user_id,InviteExpiresUtc=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE),ExpiresUtc=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE) WHERE BattleId=p_battle_id AND CreatorUserId=p_creator_user_id AND Status='waiting';
    IF ROW_COUNT()<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The battle could not be sent as an invitation.'; END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_bot_join//
CREATE PROCEDURE sp_case_battles_bot_join(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_creator CHAR(36) DEFAULT NULL; DECLARE v_creator_count INT DEFAULT 0;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattleBotSettings WHERE SettingsId=1 AND Enabled=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot is disabled.'; END IF;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting') OR (SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id)<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle cannot be joined by Battle Bot.'; END IF;
    SELECT CreatorUserId INTO v_creator FROM CaseOpeningBattles WHERE BattleId=p_battle_id;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_creator_count FROM CaseOpeningBattles battle LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId WHERE (battle.Status='opening' OR (battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6))) AND (battle.CreatorUserId=v_creator OR battle.InvitedUserId=v_creator OR participant.UserId=v_creator);
    IF v_creator_count>5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You can have up to five unresolved case battles at once.'; END IF;
    INSERT INTO CaseOpeningBattleBotAcquisitions(BattleId,CaseKey,Quantity) SELECT p_battle_id,CaseKey,COUNT(*) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey;
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) SELECT '00000000-0000-0000-0000-000000000001',battleCase.CaseKey,COUNT(*),UTC_TIMESTAMP(6) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=p_battle_id GROUP BY battleCase.CaseKey ON DUPLICATE KEY UPDATE Quantity=CaseOpeningOwnedCases.Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
    CALL sp_case_battles_join(p_battle_id,'00000000-0000-0000-0000-000000000001');
    UPDATE CaseOpeningBattleParticipants SET IsReady=1 WHERE BattleId=p_battle_id AND UserId='00000000-0000-0000-0000-000000000001';
    UPDATE CaseOpeningBattleBotStats SET BattlesAttempted=BattlesAttempted+1,UpdatedUtc=UTC_TIMESTAMP(6) WHERE StatsId=1;
END//
DELIMITER ;

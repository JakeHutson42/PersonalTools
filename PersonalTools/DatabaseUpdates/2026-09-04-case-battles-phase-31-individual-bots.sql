-- Lets each system bot occupy one explicitly selected seat so bots and invited users can mix.
-- Keeping bots as participant rows leaves seat/team assignment available for the planned 2v2 mode.
USE PersonalTools;

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_bot_join//
CREATE PROCEDURE sp_case_battles_bot_join(IN p_battle_id CHAR(36), IN p_bot_user_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT NULL;
    DECLARE v_required_players INT DEFAULT 0;
    DECLARE v_joined_players INT DEFAULT 0;
    DECLARE v_existing_bots INT DEFAULT 0;

    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattleBotSettings WHERE SettingsId=1 AND Enabled=1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot is disabled.';
    END IF;
    IF p_bot_user_id NOT IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That Battle Bot is unavailable.';
    END IF;

    SELECT Mode INTO v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting';
    SET v_required_players=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 WHEN 'ffa-4' THEN 4 ELSE 0 END;
    SELECT COUNT(*) INTO v_joined_players FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_existing_bots FROM CaseOpeningBattleParticipants
    WHERE BattleId=p_battle_id AND UserId IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003');
    IF v_required_players=0 OR v_joined_players>=v_required_players
       OR EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId=p_bot_user_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot cannot occupy that seat in this battle.';
    END IF;

    INSERT INTO CaseOpeningBattleBotAcquisitions(BattleId,CaseKey,Quantity)
    SELECT p_battle_id,CaseKey,COUNT(*) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey
    ON DUPLICATE KEY UPDATE Quantity=CaseOpeningBattleBotAcquisitions.Quantity+VALUES(Quantity);
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
    SELECT p_bot_user_id,CaseKey,COUNT(*),UTC_TIMESTAMP(6) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey
    ON DUPLICATE KEY UPDATE Quantity=CaseOpeningOwnedCases.Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
    CALL sp_case_battles_join(p_battle_id,p_bot_user_id);
    UPDATE CaseOpeningBattleParticipants SET IsReady=1 WHERE BattleId=p_battle_id AND UserId=p_bot_user_id;
    IF v_existing_bots=0 THEN
        UPDATE CaseOpeningBattleBotStats SET BattlesAttempted=BattlesAttempted+1,UpdatedUtc=UTC_TIMESTAMP(6) WHERE StatsId=1;
    END IF;
END//

DELIMITER ;

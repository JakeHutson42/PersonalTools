-- Battle Bot now buys its stake and joins through the same escrow procedure as a user opponent.
USE PersonalTools;
DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_bot_join//
CREATE PROCEDURE sp_case_battles_bot_join(IN p_battle_id CHAR(36))
BEGIN
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattleBotSettings WHERE SettingsId=1 AND Enabled=1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot is disabled.';
    END IF;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting')
       OR (SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id)<>1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle cannot be joined by Battle Bot.';
    END IF;

    -- Provision exactly the required stake, then let the ordinary join procedure validate,
    -- reserve case inventory and reserve overflow capacity exactly as it does for a person.
    INSERT INTO CaseOpeningBattleBotAcquisitions(BattleId,CaseKey,Quantity)
    SELECT p_battle_id,CaseKey,COUNT(*)
    FROM CaseOpeningBattleCases
    WHERE BattleId=p_battle_id
    GROUP BY CaseKey;
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
    SELECT '00000000-0000-0000-0000-000000000001',CaseKey,COUNT(*),UTC_TIMESTAMP(6)
    FROM CaseOpeningBattleCases
    WHERE BattleId=p_battle_id
    GROUP BY CaseKey
    ON DUPLICATE KEY UPDATE Quantity=Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);

    CALL sp_case_battles_join(p_battle_id,'00000000-0000-0000-0000-000000000001');
    UPDATE CaseOpeningBattleParticipants
    SET IsReady=1
    WHERE BattleId=p_battle_id AND UserId='00000000-0000-0000-0000-000000000001';
    UPDATE CaseOpeningBattleBotStats
    SET BattlesAttempted=BattlesAttempted+1,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE StatsId=1;
END//
DELIMITER ;

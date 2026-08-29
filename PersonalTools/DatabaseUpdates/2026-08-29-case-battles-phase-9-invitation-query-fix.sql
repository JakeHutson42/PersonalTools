-- MariaDB does not permit the outer battle alias inside a nested derived table.
-- This replacement preserves ordered case keys without that invalid correlation.
USE PersonalTools;
DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_invitation_get//
CREATE PROCEDURE sp_case_battles_invitation_get(IN p_user_id CHAR(36))
BEGIN
    DECLARE v_battle_id CHAR(36) DEFAULT NULL;
    SELECT BattleId INTO v_battle_id
    FROM CaseOpeningBattles
    WHERE InvitedUserId=p_user_id AND Status='waiting'
    ORDER BY CreatedUtc DESC
    LIMIT 1;

    IF v_battle_id IS NOT NULL THEN
        CALL sp_case_battles_invitation_expire(v_battle_id);
    END IF;

    SELECT battle.BattleId,creator.DisplayName AS CreatorDisplayName,
           (SELECT JSON_ARRAYAGG(battleCase.CaseKey ORDER BY battleCase.RoundNumber)
            FROM CaseOpeningBattleCases battleCase
            WHERE battleCase.BattleId=battle.BattleId) AS CaseKeys,
           battle.InviteExpiresUtc AS ExpiresUtc
    FROM CaseOpeningBattles battle
    INNER JOIN Users creator ON creator.UserId=battle.CreatorUserId
    WHERE battle.InvitedUserId=p_user_id
      AND battle.Status='waiting'
      AND battle.InviteExpiresUtc>UTC_TIMESTAMP(6)
    LIMIT 1;
END//
DELIMITER ;

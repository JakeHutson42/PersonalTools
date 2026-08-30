-- Expose a creator's live invitations so they remain cancellable after leaving a battle room.
USE PersonalTools;
DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_pending_created_get//
CREATE PROCEDURE sp_case_battles_pending_created_get(IN p_user_id CHAR(36))
BEGIN
    SELECT battle.BattleId,COALESCE(opponent.DisplayName,'Waiting for opponent') AS OpponentDisplayName,
           (SELECT COUNT(*) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=battle.BattleId) AS CaseCount,
           battle.ExpiresUtc
    FROM CaseOpeningBattles battle
    LEFT JOIN Users opponent ON opponent.UserId=battle.InvitedUserId
    WHERE battle.CreatorUserId=p_user_id
      AND battle.Status='waiting'
      AND battle.ExpiresUtc>UTC_TIMESTAMP(6)
    ORDER BY battle.CreatedUtc DESC
    LIMIT 5;
END//
DELIMITER ;

-- Exposes the already-locked wear variant for battle reels and result cards.
USE PersonalTools;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_pulls_get//
CREATE PROCEDURE sp_case_battles_pulls_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT roll.BattleRollId,roll.OriginalOwnerUserId,user.DisplayName OriginalOwnerDisplayName,roll.RoundNumber,
        roll.ItemName,roll.Wear,roll.ImageUrl,roll.RarityColor,roll.LockedValue,settlement.Delivery
    FROM CaseOpeningBattleRolls roll
    INNER JOIN Users user ON user.UserId=roll.OriginalOwnerUserId
    LEFT JOIN CaseOpeningBattleSettlements settlement ON settlement.BattleRollId=roll.BattleRollId
    WHERE roll.BattleId=p_battle_id
      AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=p_battle_id AND me.UserId=p_user_id)
    ORDER BY roll.RoundNumber,roll.BattleRollId;
END//
DELIMITER ;

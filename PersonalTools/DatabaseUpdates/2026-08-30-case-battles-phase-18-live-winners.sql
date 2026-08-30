-- A daily hall of fame view for the simulator's shared live-winners panel.
USE PersonalTools;
DELIMITER //
DROP PROCEDURE IF EXISTS sp_live_winners_case_battles_top_get//
CREATE PROCEDURE sp_live_winners_case_battles_top_get()
BEGIN
    SELECT battle.BattleId,user.DisplayName,participant.AwardedValue,
           (SELECT COUNT(*) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=battle.BattleId) AS CaseCount,
           battle.SettledUtc
    FROM CaseOpeningBattles battle
    INNER JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId AND participant.UserId=battle.WinningUserId
    INNER JOIN Users user ON user.UserId=battle.WinningUserId
    WHERE battle.Status='settled' AND battle.SettledUtc>=UTC_DATE()
    ORDER BY participant.AwardedValue DESC,battle.SettledUtc DESC
    LIMIT 5;
END//
DELIMITER ;

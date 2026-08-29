-- Phase 5: operational visibility only. Running this does not change a battle winner or staged result.
USE PersonalTools;
DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_admin_reconciliation_get//
CREATE PROCEDURE sp_case_battles_admin_reconciliation_get()
BEGIN
    SELECT battle.BattleId,battle.Status,battle.Mode,creator.DisplayName AS CreatorDisplayName,
           COUNT(DISTINCT participant.UserId) AS JoinedPlayers,COUNT(DISTINCT battleCase.RoundNumber) AS CaseCount,
           COUNT(DISTINCT CONCAT(reservation.UserId,':',reservation.CaseKey)) AS ReservedCaseCount,COUNT(DISTINCT roll.BattleRollId) AS StagedRollCount,
           battle.CreatedUtc,battle.ExpiresUtc,battle.StartedUtc,
           CASE WHEN battle.Status='waiting' AND battle.ExpiresUtc<=UTC_TIMESTAMP(6) THEN 'expired-waiting'
                WHEN battle.Status='opening' THEN 'opening-recovery-required' ELSE 'unresolved' END AS Attention
    FROM CaseOpeningBattles battle
    INNER JOIN Users creator ON creator.UserId=battle.CreatorUserId
    LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId
    LEFT JOIN CaseOpeningBattleCases battleCase ON battleCase.BattleId=battle.BattleId
    LEFT JOIN CaseOpeningBattleCaseReservations reservation ON reservation.BattleId=battle.BattleId AND reservation.ReleasedUtc IS NULL
    LEFT JOIN CaseOpeningBattleRolls roll ON roll.BattleId=battle.BattleId
    WHERE (battle.Status='waiting' AND battle.ExpiresUtc<=UTC_TIMESTAMP(6)) OR battle.Status='opening'
    GROUP BY battle.BattleId,battle.Status,battle.Mode,creator.DisplayName,battle.CreatedUtc,battle.ExpiresUtc,battle.StartedUtc
    ORDER BY battle.CreatedUtc ASC;
END//
DELIMITER ;

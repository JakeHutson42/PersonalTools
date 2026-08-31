-- Adds each participant's public Case Tycoon avatar to battle room/detail responses.
USE PersonalTools;
DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_participants_get//
CREATE PROCEDURE sp_case_battles_participants_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT participant.UserId,user.DisplayName,
        COALESCE(profile.SettingValue,IF(BINARY participant.UserId=BINARY '00000000-0000-0000-0000-000000000001','🤖','😎')) ProfileAvatar,
        participant.Seat,participant.Team,participant.IsReady,participant.TotalValue,participant.OverflowReservedSlots
    FROM CaseOpeningBattleParticipants participant
    INNER JOIN Users user ON user.UserId=participant.UserId
    LEFT JOIN AppSettings profile ON profile.UserId=participant.UserId AND profile.SettingKey='CaseProfileEmoji'
    WHERE participant.BattleId=p_battle_id
      AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=p_battle_id AND me.UserId=p_user_id)
    ORDER BY participant.Seat;
END//

DELIMITER ;

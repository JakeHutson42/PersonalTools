-- Phase 30 lets a pending invitee preview the lobby before purchasing and accepting.
-- Read access remains limited to joined participants or the specific active invitee.
USE PersonalTools;

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_participants_get//
CREATE PROCEDURE sp_case_battles_participants_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT participant.UserId,user.DisplayName,
        COALESCE(profile.SettingValue,IF(participant.UserId IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003'),'🤖','😎')) ProfileAvatar,
        participant.Seat,participant.Team,participant.IsReady,participant.TotalValue,participant.OverflowReservedSlots
    FROM CaseOpeningBattleParticipants participant
    INNER JOIN Users user ON user.UserId=participant.UserId
    LEFT JOIN AppSettings profile ON profile.UserId=participant.UserId AND profile.SettingKey='CaseProfileEmoji'
    WHERE participant.BattleId=p_battle_id
      AND (EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=p_battle_id AND me.UserId=p_user_id)
        OR EXISTS(SELECT 1 FROM CaseOpeningBattleInvitations invitation INNER JOIN CaseOpeningBattles battle ON battle.BattleId=invitation.BattleId
            WHERE invitation.BattleId=p_battle_id AND invitation.InvitedUserId=p_user_id AND invitation.Status='pending'
              AND invitation.ExpiresUtc>UTC_TIMESTAMP(6) AND battle.Status='waiting'))
    ORDER BY participant.Seat;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitation_states_get//
CREATE PROCEDURE sp_case_battles_invitation_states_get(IN p_battle_id CHAR(36),IN p_user_id CHAR(36))
BEGIN
    SELECT invitation.InvitedUserId,user.DisplayName,invitation.Status,invitation.ExpiresUtc
    FROM CaseOpeningBattleInvitations invitation
    INNER JOIN Users user ON user.UserId=invitation.InvitedUserId
    WHERE invitation.BattleId=p_battle_id
      AND (EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants participant WHERE participant.BattleId=p_battle_id AND participant.UserId=p_user_id)
        OR EXISTS(SELECT 1 FROM CaseOpeningBattleInvitations mine INNER JOIN CaseOpeningBattles battle ON battle.BattleId=mine.BattleId
            WHERE mine.BattleId=p_battle_id AND mine.InvitedUserId=p_user_id AND mine.Status='pending'
              AND mine.ExpiresUtc>UTC_TIMESTAMP(6) AND battle.Status='waiting'))
    ORDER BY invitation.InvitedUtc,invitation.InvitedUserId;
END//

DELIMITER ;

-- Targeted 1v1 invitations. A browser timer is only a convenience; these procedures enforce expiry.
USE PersonalTools;
ALTER TABLE CaseOpeningBattles
    ADD COLUMN IF NOT EXISTS InvitedUserId CHAR(36) NULL AFTER CreatorUserId,
    ADD COLUMN IF NOT EXISTS InviteExpiresUtc DATETIME(6) NULL AFTER ExpiresUtc,
    ADD KEY IF NOT EXISTS IX_CaseOpeningBattles_Invitation (InvitedUserId, Status, InviteExpiresUtc),
    ADD CONSTRAINT FK_CaseOpeningBattles_InvitedUser FOREIGN KEY (InvitedUserId) REFERENCES Users(UserId) ON DELETE SET NULL;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_invitable_users_get//
CREATE PROCEDURE sp_case_battles_invitable_users_get(IN p_user_id CHAR(36))
BEGIN
    SELECT UserId,DisplayName FROM Users WHERE IsActive=1 AND BINARY UserId<>BINARY p_user_id ORDER BY DisplayName;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invite_set//
CREATE PROCEDURE sp_case_battles_invite_set(IN p_battle_id CHAR(36), IN p_creator_user_id CHAR(36), IN p_invited_user_id CHAR(36))
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM Users WHERE UserId=p_invited_user_id AND IsActive=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user is no longer available to invite.'; END IF;
    IF EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants p INNER JOIN CaseOpeningBattles b ON b.BattleId=p.BattleId WHERE p.UserId=p_invited_user_id AND b.Status IN ('waiting','opening')) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user is already in an unresolved battle.'; END IF;
    UPDATE CaseOpeningBattles SET InvitedUserId=p_invited_user_id,InviteExpiresUtc=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 15 SECOND),ExpiresUtc=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 15 SECOND)
    WHERE BattleId=p_battle_id AND CreatorUserId=p_creator_user_id AND Status='waiting';
    IF ROW_COUNT()<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The battle could not be sent as an invitation.'; END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitation_expire//
CREATE PROCEDURE sp_case_battles_invitation_expire(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_status VARCHAR(16) DEFAULT '';
    DECLARE v_expiry DATETIME(6) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Status,InviteExpiresUtc INTO v_status,v_expiry FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE;
    IF v_status='waiting' AND v_expiry IS NOT NULL AND v_expiry<=UTC_TIMESTAMP(6) THEN
        UPDATE CaseOpeningBattles SET Status='cancelled',CancelledUtc=UTC_TIMESTAMP(6),SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
        CALL sp_case_battles_reservations_release(p_battle_id);
    END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitation_get//
CREATE PROCEDURE sp_case_battles_invitation_get(IN p_user_id CHAR(36))
BEGIN
    DECLARE v_battle_id CHAR(36) DEFAULT NULL;
    SELECT BattleId INTO v_battle_id FROM CaseOpeningBattles WHERE InvitedUserId=p_user_id AND Status='waiting' ORDER BY CreatedUtc DESC LIMIT 1;
    IF v_battle_id IS NOT NULL THEN CALL sp_case_battles_invitation_expire(v_battle_id); END IF;
    SELECT battle.BattleId,creator.DisplayName AS CreatorDisplayName,
           (SELECT JSON_ARRAYAGG(battleCase.CaseKey ORDER BY battleCase.RoundNumber) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=battle.BattleId) AS CaseKeys,battle.InviteExpiresUtc AS ExpiresUtc
    FROM CaseOpeningBattles battle INNER JOIN Users creator ON creator.UserId=battle.CreatorUserId
    WHERE battle.InvitedUserId=p_user_id AND battle.Status='waiting' AND battle.InviteExpiresUtc>UTC_TIMESTAMP(6) LIMIT 1;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitation_accept//
CREATE PROCEDURE sp_case_battles_invitation_accept(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND InvitedUserId=p_user_id AND Status='waiting' AND InviteExpiresUtc>UTC_TIMESTAMP(6)) THEN
        CALL sp_case_battles_invitation_expire(p_battle_id);
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This invitation has expired or is unavailable.';
    END IF;
    CALL sp_case_battles_join(p_battle_id,p_user_id);
    UPDATE CaseOpeningBattles SET InviteExpiresUtc=NULL,ExpiresUtc=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE) WHERE BattleId=p_battle_id AND Status='waiting';
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitation_decline//
CREATE PROCEDURE sp_case_battles_invitation_decline(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND InvitedUserId=p_user_id AND Status='waiting') THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This invitation is unavailable.'; END IF;
    UPDATE CaseOpeningBattles SET Status='cancelled',CancelledUtc=UTC_TIMESTAMP(6),SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
    CALL sp_case_battles_reservations_release(p_battle_id);
    COMMIT;
END//
DELIMITER ;

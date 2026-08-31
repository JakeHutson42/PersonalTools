-- Phase 1 of three-player free-for-all battles. Invitations become first-class rows so a
-- battle owner can invite every seat and re-invite a declined or expired seat safely.
-- The mode flag is persisted separately, but application execution remains disabled until
-- the later service and UI phases are deployed.
USE PersonalTools;

ALTER TABLE CaseOpeningBattleBotSettings
    ADD COLUMN IF NOT EXISTS FreeForAll3Enabled TINYINT(1) NOT NULL DEFAULT 0 AFTER CaseBattlesEnabled;

CREATE TABLE IF NOT EXISTS CaseOpeningBattleInvitations
(
    BattleId CHAR(36) NOT NULL,
    InvitedUserId CHAR(36) NOT NULL,
    Status VARCHAR(16) NOT NULL DEFAULT 'pending',
    InvitedUtc DATETIME(6) NOT NULL,
    ExpiresUtc DATETIME(6) NOT NULL,
    RespondedUtc DATETIME(6) NULL,
    PRIMARY KEY (BattleId, InvitedUserId),
    KEY IX_CaseOpeningBattleInvitations_User_Status (InvitedUserId, Status, ExpiresUtc),
    CONSTRAINT FK_CaseOpeningBattleInvitations_Battle FOREIGN KEY (BattleId) REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE,
    CONSTRAINT FK_CaseOpeningBattleInvitations_User FOREIGN KEY (InvitedUserId) REFERENCES Users(UserId) ON DELETE CASCADE
) ENGINE=InnoDB;

-- Preserve all unresolved legacy 1v1 invitations before procedures switch to the new table.
INSERT INTO CaseOpeningBattleInvitations(BattleId,InvitedUserId,Status,InvitedUtc,ExpiresUtc,RespondedUtc)
SELECT battle.BattleId,battle.InvitedUserId,
       CASE WHEN participant.UserId IS NOT NULL THEN 'accepted'
            WHEN battle.Status='waiting' AND battle.InviteExpiresUtc>UTC_TIMESTAMP(6) THEN 'pending'
            WHEN battle.Status='waiting' THEN 'expired' ELSE 'cancelled' END,
       battle.CreatedUtc,COALESCE(battle.InviteExpiresUtc,battle.ExpiresUtc),
       CASE WHEN participant.UserId IS NOT NULL OR battle.Status<>'waiting' OR battle.InviteExpiresUtc<=UTC_TIMESTAMP(6) THEN UTC_TIMESTAMP(6) ELSE NULL END
FROM CaseOpeningBattles battle
LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId AND participant.UserId=battle.InvitedUserId
WHERE battle.InvitedUserId IS NOT NULL
ON DUPLICATE KEY UPDATE Status=VALUES(Status),ExpiresUtc=VALUES(ExpiresUtc),RespondedUtc=VALUES(RespondedUtc);

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_bot_status_get//
CREATE PROCEDURE sp_case_battles_bot_status_get()
BEGIN
    SELECT settings.CaseBattlesEnabled,settings.FreeForAll3Enabled,settings.Enabled,
           stats.BattlesAttempted,stats.BattlesWon,stats.SkinsDiscarded,stats.ValueDiscarded
    FROM CaseOpeningBattleBotSettings settings
    INNER JOIN CaseOpeningBattleBotStats stats ON stats.StatsId=1
    WHERE settings.SettingsId=1;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_ffa3_enabled_set//
CREATE PROCEDURE sp_case_battles_ffa3_enabled_set(IN p_enabled TINYINT)
BEGIN
    UPDATE CaseOpeningBattleBotSettings
    SET FreeForAll3Enabled=IF(p_enabled<>0,1,0),UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE SettingsId=1;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invite_set//
CREATE PROCEDURE sp_case_battles_invite_set(IN p_battle_id CHAR(36),IN p_creator_user_id CHAR(36),IN p_invited_user_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT NULL;
    DECLARE v_required_players INT DEFAULT 0;
    DECLARE v_reserved_opponent_seats INT DEFAULT 0;
    DECLARE v_creator_count INT DEFAULT 0;
    DECLARE v_invited_count INT DEFAULT 0;
    DECLARE v_expiry DATETIME(6) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;

    SELECT Mode INTO v_mode FROM CaseOpeningBattles
    WHERE BattleId=p_battle_id AND CreatorUserId=p_creator_user_id AND Status='waiting'
    FOR UPDATE;
    IF v_mode IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Only the battle owner can invite players to a waiting battle.'; END IF;
    SET v_required_players=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 WHEN 'ffa-4' THEN 4 WHEN 'teams-2v2' THEN 4 ELSE 0 END;
    IF v_required_players=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle mode cannot accept invitations.'; END IF;
    IF p_invited_user_id=p_creator_user_id OR NOT EXISTS(SELECT 1 FROM Users WHERE UserId=p_invited_user_id AND IsActive=1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user is no longer available to invite.';
    END IF;
    IF EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId=p_invited_user_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user has already joined this battle.';
    END IF;

    SELECT COUNT(*) INTO v_reserved_opponent_seats
    FROM CaseOpeningBattleInvitations invitation
    WHERE invitation.BattleId=p_battle_id AND invitation.Status IN ('pending','accepted')
      AND invitation.InvitedUserId<>p_invited_user_id
      AND (invitation.Status='accepted' OR invitation.ExpiresUtc>UTC_TIMESTAMP(6));
    IF v_reserved_opponent_seats>=v_required_players-1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Every opponent seat already has an active invitation.'; END IF;

    SELECT COUNT(DISTINCT battle.BattleId) INTO v_creator_count
    FROM CaseOpeningBattles battle
    LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId
    LEFT JOIN CaseOpeningBattleInvitations invitation ON invitation.BattleId=battle.BattleId
    WHERE (battle.Status='opening' OR (battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6)))
      AND (battle.CreatorUserId=p_creator_user_id OR participant.UserId=p_creator_user_id OR (invitation.InvitedUserId=p_creator_user_id AND invitation.Status IN ('pending','accepted') AND (invitation.Status='accepted' OR invitation.ExpiresUtc>UTC_TIMESTAMP(6))));
    IF v_creator_count>5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You can have up to five unresolved case battles at once.'; END IF;

    SELECT COUNT(DISTINCT battle.BattleId) INTO v_invited_count
    FROM CaseOpeningBattles battle
    LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId
    LEFT JOIN CaseOpeningBattleInvitations invitation ON invitation.BattleId=battle.BattleId
    WHERE (battle.Status='opening' OR (battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6)))
      AND (battle.CreatorUserId=p_invited_user_id OR participant.UserId=p_invited_user_id OR (invitation.InvitedUserId=p_invited_user_id AND invitation.Status IN ('pending','accepted') AND (invitation.Status='accepted' OR invitation.ExpiresUtc>UTC_TIMESTAMP(6))));
    IF v_invited_count>=5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user already has five unresolved case battles.'; END IF;

    SET v_expiry=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE);
    INSERT INTO CaseOpeningBattleInvitations(BattleId,InvitedUserId,Status,InvitedUtc,ExpiresUtc,RespondedUtc)
    VALUES(p_battle_id,p_invited_user_id,'pending',UTC_TIMESTAMP(6),v_expiry,NULL)
    ON DUPLICATE KEY UPDATE Status='pending',InvitedUtc=VALUES(InvitedUtc),ExpiresUtc=VALUES(ExpiresUtc),RespondedUtc=NULL;
    -- A three-player owner gets a recovery window after an invite expires so they can
    -- re-invite that seat or cancel the battle instead of losing the room immediately.
    UPDATE CaseOpeningBattles
    SET ExpiresUtc=GREATEST(ExpiresUtc,IF(v_mode='ffa-3',DATE_ADD(v_expiry,INTERVAL 20 MINUTE),v_expiry))
    WHERE BattleId=p_battle_id;
    -- Keep the legacy columns populated for older duel deployments during a rolling release.
    IF v_mode='duel' THEN
        UPDATE CaseOpeningBattles SET InvitedUserId=p_invited_user_id,InviteExpiresUtc=v_expiry WHERE BattleId=p_battle_id;
    END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitations_get//
CREATE PROCEDURE sp_case_battles_invitations_get(IN p_user_id CHAR(36))
BEGIN
    UPDATE CaseOpeningBattleInvitations invitation
    INNER JOIN CaseOpeningBattles battle ON battle.BattleId=invitation.BattleId
    SET invitation.Status='expired',invitation.RespondedUtc=UTC_TIMESTAMP(6)
    WHERE invitation.InvitedUserId=p_user_id AND invitation.Status='pending'
      AND (invitation.ExpiresUtc<=UTC_TIMESTAMP(6) OR battle.Status<>'waiting');
    SELECT battle.BattleId,creator.DisplayName AS CreatorDisplayName,
           (SELECT JSON_ARRAYAGG(battleCase.CaseKey ORDER BY battleCase.RoundNumber) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=battle.BattleId) AS CaseKeys,
           invitation.ExpiresUtc
    FROM CaseOpeningBattleInvitations invitation
    INNER JOIN CaseOpeningBattles battle ON battle.BattleId=invitation.BattleId
    INNER JOIN Users creator ON creator.UserId=battle.CreatorUserId
    WHERE invitation.InvitedUserId=p_user_id AND invitation.Status='pending'
      AND invitation.ExpiresUtc>UTC_TIMESTAMP(6) AND battle.Status='waiting'
    ORDER BY invitation.InvitedUtc ASC LIMIT 5;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_pending_created_get//
CREATE PROCEDURE sp_case_battles_pending_created_get(IN p_user_id CHAR(36))
BEGIN
    SELECT battle.BattleId,
           COALESCE(GROUP_CONCAT(invitedUser.DisplayName ORDER BY invitation.InvitedUtc SEPARATOR ', '),'Waiting for opponents') AS OpponentDisplayName,
           (SELECT COUNT(*) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=battle.BattleId) AS CaseCount,
           battle.ExpiresUtc
    FROM CaseOpeningBattles battle
    LEFT JOIN CaseOpeningBattleInvitations invitation ON invitation.BattleId=battle.BattleId AND invitation.Status IN ('pending','accepted')
    LEFT JOIN Users invitedUser ON invitedUser.UserId=invitation.InvitedUserId
    WHERE battle.CreatorUserId=p_user_id AND battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6)
    GROUP BY battle.BattleId,battle.ExpiresUtc,battle.CreatedUtc
    ORDER BY battle.CreatedUtc DESC LIMIT 5;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitation_accept//
CREATE PROCEDURE sp_case_battles_invitation_accept(IN p_battle_id CHAR(36),IN p_user_id CHAR(36))
BEGIN
    DECLARE v_available INT DEFAULT 0;
    SELECT COUNT(*) INTO v_available
    FROM CaseOpeningBattleInvitations invitation
    INNER JOIN CaseOpeningBattles battle ON battle.BattleId=invitation.BattleId
    WHERE invitation.BattleId=p_battle_id AND invitation.InvitedUserId=p_user_id
      AND invitation.Status='pending' AND invitation.ExpiresUtc>UTC_TIMESTAMP(6) AND battle.Status='waiting';
    IF v_available<>1 THEN
        UPDATE CaseOpeningBattleInvitations SET Status='expired',RespondedUtc=UTC_TIMESTAMP(6)
        WHERE BattleId=p_battle_id AND InvitedUserId=p_user_id AND Status='pending' AND ExpiresUtc<=UTC_TIMESTAMP(6);
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This invitation has expired or is unavailable.';
    END IF;
    CALL sp_case_battles_join(p_battle_id,p_user_id);
    UPDATE CaseOpeningBattleInvitations SET Status='accepted',RespondedUtc=UTC_TIMESTAMP(6)
    WHERE BattleId=p_battle_id AND InvitedUserId=p_user_id AND Status='pending';
    UPDATE CaseOpeningBattles SET InviteExpiresUtc=IF(Mode='duel',NULL,InviteExpiresUtc),ExpiresUtc=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE)
    WHERE BattleId=p_battle_id AND Status='waiting';
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitation_decline//
CREATE PROCEDURE sp_case_battles_invitation_decline(IN p_battle_id CHAR(36),IN p_user_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT battle.Mode INTO v_mode
    FROM CaseOpeningBattleInvitations invitation
    INNER JOIN CaseOpeningBattles battle ON battle.BattleId=invitation.BattleId
    WHERE invitation.BattleId=p_battle_id AND invitation.InvitedUserId=p_user_id
      AND invitation.Status='pending' AND battle.Status='waiting' FOR UPDATE;
    IF v_mode IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This invitation is unavailable.'; END IF;
    UPDATE CaseOpeningBattleInvitations SET Status='declined',RespondedUtc=UTC_TIMESTAMP(6)
    WHERE BattleId=p_battle_id AND InvitedUserId=p_user_id;
    -- Preserve current duel behaviour. Multi-seat battles stay open for owner re-invitation.
    IF v_mode='duel' THEN
        UPDATE CaseOpeningBattles SET Status='cancelled',CancelledUtc=UTC_TIMESTAMP(6),SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
        CALL sp_case_battles_reservations_release(p_battle_id);
    END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitation_expire//
CREATE PROCEDURE sp_case_battles_invitation_expire(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Mode INTO v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting' FOR UPDATE;
    UPDATE CaseOpeningBattleInvitations SET Status='expired',RespondedUtc=UTC_TIMESTAMP(6)
    WHERE BattleId=p_battle_id AND Status='pending' AND ExpiresUtc<=UTC_TIMESTAMP(6);
    IF v_mode='duel' AND NOT EXISTS(SELECT 1 FROM CaseOpeningBattleInvitations WHERE BattleId=p_battle_id AND Status IN ('pending','accepted')) THEN
        UPDATE CaseOpeningBattles SET Status='cancelled',CancelledUtc=UTC_TIMESTAMP(6),SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
        CALL sp_case_battles_reservations_release(p_battle_id);
    END IF;
    COMMIT;
END//

DELIMITER ;

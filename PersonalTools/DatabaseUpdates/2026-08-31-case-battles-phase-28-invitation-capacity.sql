-- Keep declined/expired invitation records for auditability, but only active unresolved
-- battles count toward the raised per-player safety limit.
USE PersonalTools;
DELIMITER //
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
    SELECT Mode INTO v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND CreatorUserId=p_creator_user_id AND Status='waiting' FOR UPDATE;
    IF v_mode IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Only the battle owner can invite players to a waiting battle.'; END IF;
    SET v_required_players=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 WHEN 'ffa-4' THEN 4 WHEN 'teams-2v2' THEN 4 ELSE 0 END;
    IF v_required_players=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle mode cannot accept invitations.'; END IF;
    IF p_invited_user_id=p_creator_user_id OR NOT EXISTS(SELECT 1 FROM Users WHERE UserId=p_invited_user_id AND IsActive=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user is no longer available to invite.'; END IF;
    IF EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId=p_invited_user_id) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user has already joined this battle.'; END IF;
    SELECT COUNT(*) INTO v_reserved_opponent_seats FROM CaseOpeningBattleInvitations invitation WHERE invitation.BattleId=p_battle_id AND invitation.Status IN ('pending','accepted') AND invitation.InvitedUserId<>p_invited_user_id AND (invitation.Status='accepted' OR invitation.ExpiresUtc>UTC_TIMESTAMP(6));
    IF v_reserved_opponent_seats>=v_required_players-1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Every opponent seat already has an active invitation.'; END IF;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_creator_count FROM CaseOpeningBattles battle LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId LEFT JOIN CaseOpeningBattleInvitations invitation ON invitation.BattleId=battle.BattleId WHERE (battle.Status='opening' OR (battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6))) AND (battle.CreatorUserId=p_creator_user_id OR participant.UserId=p_creator_user_id OR (invitation.InvitedUserId=p_creator_user_id AND invitation.Status IN ('pending','accepted') AND (invitation.Status='accepted' OR invitation.ExpiresUtc>UTC_TIMESTAMP(6))));
    IF v_creator_count>=20 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You can have up to twenty unresolved case battles at once.'; END IF;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_invited_count FROM CaseOpeningBattles battle LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId LEFT JOIN CaseOpeningBattleInvitations invitation ON invitation.BattleId=battle.BattleId WHERE (battle.Status='opening' OR (battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6))) AND (battle.CreatorUserId=p_invited_user_id OR participant.UserId=p_invited_user_id OR (invitation.InvitedUserId=p_invited_user_id AND invitation.Status IN ('pending','accepted') AND (invitation.Status='accepted' OR invitation.ExpiresUtc>UTC_TIMESTAMP(6))));
    IF v_invited_count>=20 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user already has twenty unresolved case battles.'; END IF;
    SET v_expiry=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE);
    INSERT INTO CaseOpeningBattleInvitations(BattleId,InvitedUserId,Status,InvitedUtc,ExpiresUtc,RespondedUtc) VALUES(p_battle_id,p_invited_user_id,'pending',UTC_TIMESTAMP(6),v_expiry,NULL) ON DUPLICATE KEY UPDATE Status='pending',InvitedUtc=VALUES(InvitedUtc),ExpiresUtc=VALUES(ExpiresUtc),RespondedUtc=NULL;
    UPDATE CaseOpeningBattles SET ExpiresUtc=GREATEST(ExpiresUtc,IF(v_mode='ffa-3',DATE_ADD(v_expiry,INTERVAL 20 MINUTE),v_expiry)) WHERE BattleId=p_battle_id;
    IF v_mode='duel' THEN UPDATE CaseOpeningBattles SET InvitedUserId=p_invited_user_id,InviteExpiresUtc=v_expiry WHERE BattleId=p_battle_id; END IF;
    COMMIT;
END//
DELIMITER ;

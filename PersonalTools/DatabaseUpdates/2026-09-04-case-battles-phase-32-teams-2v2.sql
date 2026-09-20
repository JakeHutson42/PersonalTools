-- Enables 2v2 execution, balances mixed human/bot teams, and liquidates the pot into an exact 50/50 GBP split.
USE PersonalTools;

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_bot_join//
CREATE PROCEDURE sp_case_battles_bot_join(IN p_battle_id CHAR(36), IN p_bot_user_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT NULL;
    DECLARE v_required_players INT DEFAULT 0;
    DECLARE v_joined_players INT DEFAULT 0;
    DECLARE v_existing_bots INT DEFAULT 0;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattleBotSettings WHERE SettingsId=1 AND Enabled=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot is disabled.'; END IF;
    IF p_bot_user_id NOT IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003') THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That Battle Bot is unavailable.'; END IF;
    SELECT Mode INTO v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting';
    SET v_required_players=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 WHEN 'ffa-4' THEN 4 WHEN 'teams-2v2' THEN 4 ELSE 0 END;
    SELECT COUNT(*) INTO v_joined_players FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_existing_bots FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003');
    IF v_required_players=0 OR v_joined_players>=v_required_players OR EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId=p_bot_user_id) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot cannot occupy that seat in this battle.'; END IF;
    INSERT INTO CaseOpeningBattleBotAcquisitions(BattleId,CaseKey,Quantity)
    SELECT p_battle_id,CaseKey,COUNT(*) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey
    ON DUPLICATE KEY UPDATE Quantity=CaseOpeningBattleBotAcquisitions.Quantity+VALUES(Quantity);
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
    SELECT p_bot_user_id,CaseKey,COUNT(*),UTC_TIMESTAMP(6) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey
    ON DUPLICATE KEY UPDATE Quantity=CaseOpeningOwnedCases.Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
    CALL sp_case_battles_join(p_battle_id,p_bot_user_id);
    UPDATE CaseOpeningBattleParticipants SET IsReady=1 WHERE BattleId=p_battle_id AND UserId=p_bot_user_id;
    IF v_existing_bots=0 THEN UPDATE CaseOpeningBattleBotStats SET BattlesAttempted=BattlesAttempted+1,UpdatedUtc=UTC_TIMESTAMP(6) WHERE StatsId=1; END IF;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_start//
CREATE PROCEDURE sp_case_battles_start(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT '';
    DECLARE v_creator CHAR(36) DEFAULT NULL;
    DECLARE v_teammate CHAR(36) DEFAULT NULL;
    DECLARE v_required INT DEFAULT 0;
    DECLARE v_joined INT DEFAULT 0;
    DECLARE v_ready INT DEFAULT 0;
    DECLARE v_snapshot CHAR(36) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Mode,CreatorUserId INTO v_mode,v_creator FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting' AND ExpiresUtc>UTC_TIMESTAMP(6) FOR UPDATE;
    IF v_mode='' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle cannot be started.'; END IF;
    SET v_required=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 ELSE 4 END;
    SELECT COUNT(*),COALESCE(SUM(IsReady),0) INTO v_joined,v_ready FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id;
    IF v_joined<>v_required OR v_ready<>v_required THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Every participant must join and ready before the battle starts.'; END IF;
    IF v_mode='teams-2v2' THEN
        SELECT participant.UserId INTO v_teammate
        FROM CaseOpeningBattleParticipants participant INNER JOIN Users user ON user.UserId=participant.UserId
        WHERE participant.BattleId=p_battle_id AND BINARY participant.UserId<>BINARY v_creator
        ORDER BY user.IsActive DESC,participant.Seat ASC LIMIT 1;
        UPDATE CaseOpeningBattleParticipants SET Team=2 WHERE BattleId=p_battle_id;
        UPDATE CaseOpeningBattleParticipants SET Team=1 WHERE BattleId=p_battle_id AND UserId IN (v_creator,v_teammate);
    END IF;
    SELECT PriceSnapshotId INTO v_snapshot FROM CaseOpeningPriceSnapshots WHERE IsActive=1 ORDER BY ImportedUtc DESC LIMIT 1;
    IF v_snapshot IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='An active price snapshot is required before starting a battle.'; END IF;
    UPDATE CaseOpeningBattles SET Status='opening',PriceSnapshotId=v_snapshot,StartedUtc=UTC_TIMESTAMP(6),LockedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_participants_get//
CREATE PROCEDURE sp_case_battles_participants_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT participant.UserId,user.DisplayName,
        COALESCE(profile.SettingValue,IF(participant.UserId IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003'),'🤖','😎')) ProfileAvatar,
        participant.Seat,participant.Team,participant.IsReady,participant.TotalValue,participant.AwardedValue,participant.OverflowReservedSlots
    FROM CaseOpeningBattleParticipants participant
    INNER JOIN Users user ON user.UserId=participant.UserId
    LEFT JOIN AppSettings profile ON profile.UserId=participant.UserId AND profile.SettingKey='CaseProfileEmoji'
    WHERE participant.BattleId=p_battle_id AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=p_battle_id AND me.UserId=p_user_id)
    ORDER BY participant.Seat;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_settle_teams_2v2//
CREATE PROCEDURE sp_case_battles_settle_teams_2v2(IN p_battle_id CHAR(36))
settle: BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT '';
    DECLARE v_status VARCHAR(16) DEFAULT '';
    DECLARE v_rolls INT DEFAULT 0;
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_team1 DECIMAL(12,2) DEFAULT 0;
    DECLARE v_team2 DECIMAL(12,2) DEFAULT 0;
    DECLARE v_winning_team INT DEFAULT 0;
    DECLARE v_first_winner CHAR(36) DEFAULT NULL;
    DECLARE v_second_winner CHAR(36) DEFAULT NULL;
    DECLARE v_total_pence BIGINT DEFAULT 0;
    DECLARE v_first_share BIGINT DEFAULT 0;
    DECLARE v_second_share BIGINT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Status,Mode INTO v_status,v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE;
    IF v_status<>'opening' OR v_mode<>'teams-2v2' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Only an opening 2v2 battle can use team settlement.'; END IF;
    SELECT COUNT(*)*4 INTO v_expected FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_rolls FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
    IF v_rolls<>v_expected OR (SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id)<>4 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The verified 2v2 roll set is incomplete.'; END IF;

    UPDATE CaseOpeningBattleParticipants participant
    SET TotalValue=(SELECT COALESCE(SUM(roll.LockedValue),0) FROM CaseOpeningBattleRolls roll WHERE roll.BattleId=p_battle_id AND roll.OriginalOwnerUserId=participant.UserId)
    WHERE participant.BattleId=p_battle_id;
    SELECT COALESCE(SUM(TotalValue),0) INTO v_team1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND Team=1;
    SELECT COALESCE(SUM(TotalValue),0) INTO v_team2 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND Team=2;
    SET v_winning_team=IF(v_team1>=v_team2,1,2);
    SELECT UserId INTO v_first_winner FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND Team=v_winning_team ORDER BY Seat LIMIT 1;
    SELECT UserId INTO v_second_winner FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND Team=v_winning_team ORDER BY Seat DESC LIMIT 1;
    SELECT ROUND(COALESCE(SUM(LockedValue),0)*100) INTO v_total_pence FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
    SET v_first_share=(v_total_pence DIV 2)+(v_total_pence MOD 2);
    SET v_second_share=v_total_pence DIV 2;

    UPDATE CaseOpeningBattleParticipants SET AwardedValue=0 WHERE BattleId=p_battle_id;
    UPDATE CaseOpeningBattleParticipants SET AwardedValue=v_first_share/100 WHERE BattleId=p_battle_id AND UserId=v_first_winner;
    UPDATE CaseOpeningBattleParticipants SET AwardedValue=v_second_share/100 WHERE BattleId=p_battle_id AND UserId=v_second_winner;
    UPDATE CaseOpeningBattleRolls SET IsSoldForSplit=1,AwardedToUserId=NULL WHERE BattleId=p_battle_id;
    INSERT INTO CaseOpeningBattlePulls(BattlePullId,BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,IsSoldForSplit)
    SELECT UUID(),BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,NULL,1 FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;

    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc)
    SELECT UserId,0,0,0,0,0,0,UTC_TIMESTAMP(6) FROM CaseOpeningBattleParticipants
    WHERE BattleId=p_battle_id AND Team=v_winning_team AND UserId NOT IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003');
    UPDATE CaseOpeningProgress SET GbpPence=GbpPence+v_first_share,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE UserId=v_first_winner AND v_first_winner NOT IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003');
    UPDATE CaseOpeningProgress SET GbpPence=GbpPence+v_second_share,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE UserId=v_second_winner AND v_second_winner NOT IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003');
    INSERT INTO CaseOpeningEconomyLedger(TransactionId,UserId,EconomyMode,AmountMinor,BalanceAfterMinor,TransactionType,ReferenceType,ReferenceId,PriceSnapshotId,CreatedUtc)
    SELECT UUID(),participant.UserId,'gbp',ROUND(participant.AwardedValue*100),progress.GbpPence,'case-battle-team-payout','battle',p_battle_id,battle.PriceSnapshotId,UTC_TIMESTAMP(6)
    FROM CaseOpeningBattleParticipants participant INNER JOIN CaseOpeningProgress progress ON progress.UserId=participant.UserId INNER JOIN CaseOpeningBattles battle ON battle.BattleId=participant.BattleId
    WHERE participant.BattleId=p_battle_id AND participant.Team=v_winning_team AND participant.UserId NOT IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003');

    UPDATE CaseOpeningBattleBotStats SET BattlesWon=BattlesWon+1,
        ValueDiscarded=ValueDiscarded+COALESCE((SELECT SUM(AwardedValue) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND Team=v_winning_team AND UserId IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003')),0),UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE StatsId=1 AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND Team=v_winning_team AND UserId IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000003'));
    UPDATE CaseOpeningBattleOverflowReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
    UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
    UPDATE CaseOpeningBattleInvitations SET Status=IF(Status='accepted','accepted','cancelled'),RespondedUtc=COALESCE(RespondedUtc,UTC_TIMESTAMP(6)) WHERE BattleId=p_battle_id;
    UPDATE CaseOpeningBattles SET Status='settled',WinningUserId=NULL,WinningTeam=v_winning_team,WinnerTieBreak='team total, team 1 on exact tie; odd penny to lower seat',SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
    COMMIT;
END//

DELIMITER ;

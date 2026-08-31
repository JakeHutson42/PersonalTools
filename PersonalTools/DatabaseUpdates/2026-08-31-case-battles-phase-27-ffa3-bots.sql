-- Adds a second system bot and lets the bot join procedure fill every opponent seat in ffa-3.
USE PersonalTools;

INSERT IGNORE INTO Users(UserId,Email,DisplayName,PasswordHash,IsActive,UserRole,CreatedUtc)
VALUES('00000000-0000-0000-0000-000000000002','battle-bot-2@system.invalid','Battle Bot Bravo','SYSTEM-ACCOUNT',0,1,UTC_TIMESTAMP(6));

UPDATE Users SET DisplayName='Battle Bot Alpha' WHERE BINARY UserId=BINARY '00000000-0000-0000-0000-000000000001';

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_bot_join//
CREATE PROCEDURE sp_case_battles_bot_join(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_creator CHAR(36) DEFAULT NULL;
    DECLARE v_creator_count INT DEFAULT 0;
    DECLARE v_mode VARCHAR(16) DEFAULT NULL;
    DECLARE v_bot_count INT DEFAULT 0;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattleBotSettings WHERE SettingsId=1 AND Enabled=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot is disabled.'; END IF;
    SELECT Mode,CreatorUserId INTO v_mode,v_creator FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting';
    SET v_bot_count=CASE v_mode WHEN 'duel' THEN 1 WHEN 'ffa-3' THEN 2 ELSE 0 END;
    IF v_bot_count=0 OR (SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id)<>1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot cannot fill the available seats in this battle.';
    END IF;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_creator_count
    FROM CaseOpeningBattles battle
    LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId
    WHERE (battle.Status='opening' OR (battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6)))
      AND (battle.CreatorUserId=v_creator OR participant.UserId=v_creator);
    IF v_creator_count>5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You can have up to five unresolved case battles at once.'; END IF;

    INSERT INTO CaseOpeningBattleBotAcquisitions(BattleId,CaseKey,Quantity)
    SELECT p_battle_id,CaseKey,COUNT(*)*v_bot_count FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey;

    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
    SELECT '00000000-0000-0000-0000-000000000001',battleCase.CaseKey,COUNT(*),UTC_TIMESTAMP(6)
    FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=p_battle_id GROUP BY battleCase.CaseKey
    ON DUPLICATE KEY UPDATE Quantity=CaseOpeningOwnedCases.Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
    CALL sp_case_battles_join(p_battle_id,'00000000-0000-0000-0000-000000000001');
    UPDATE CaseOpeningBattleParticipants SET IsReady=1 WHERE BattleId=p_battle_id AND UserId='00000000-0000-0000-0000-000000000001';

    IF v_bot_count=2 THEN
        INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
        SELECT '00000000-0000-0000-0000-000000000002',battleCase.CaseKey,COUNT(*),UTC_TIMESTAMP(6)
        FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=p_battle_id GROUP BY battleCase.CaseKey
        ON DUPLICATE KEY UPDATE Quantity=CaseOpeningOwnedCases.Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
        CALL sp_case_battles_join(p_battle_id,'00000000-0000-0000-0000-000000000002');
        UPDATE CaseOpeningBattleParticipants SET IsReady=1 WHERE BattleId=p_battle_id AND UserId='00000000-0000-0000-0000-000000000002';
    END IF;
    UPDATE CaseOpeningBattleBotStats SET BattlesAttempted=BattlesAttempted+1,UpdatedUtc=UTC_TIMESTAMP(6) WHERE StatsId=1;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_participants_get//
CREATE PROCEDURE sp_case_battles_participants_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT participant.UserId,user.DisplayName,
        COALESCE(profile.SettingValue,IF(participant.UserId IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002'),'🤖','😎')) ProfileAvatar,
        participant.Seat,participant.Team,participant.IsReady,participant.TotalValue,participant.OverflowReservedSlots
    FROM CaseOpeningBattleParticipants participant
    INNER JOIN Users user ON user.UserId=participant.UserId
    LEFT JOIN AppSettings profile ON profile.UserId=participant.UserId AND profile.SettingKey='CaseProfileEmoji'
    WHERE participant.BattleId=p_battle_id
      AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=p_battle_id AND me.UserId=p_user_id)
    ORDER BY participant.Seat;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_settle_staged//
CREATE PROCEDURE sp_case_battles_settle_staged(IN p_battle_id CHAR(36))
settle: BEGIN
    DECLARE v_winner CHAR(36) DEFAULT NULL;
    DECLARE v_rolls INT DEFAULT 0;
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_required_players INT DEFAULT 0;
    DECLARE v_joined_players INT DEFAULT 0;
    DECLARE v_capacity INT DEFAULT 0;
    DECLARE v_deliver INT DEFAULT 0;
    DECLARE v_overflow INT DEFAULT 0;
    DECLARE v_status VARCHAR(16) DEFAULT '';
    DECLARE v_mode VARCHAR(16) DEFAULT '';
    DECLARE v_allocation_reason VARCHAR(40) DEFAULT '';
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;

    SELECT Status,Mode INTO v_status,v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE;
    SET v_required_players=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 ELSE 0 END;
    IF v_status<>'opening' OR v_required_players=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle mode cannot be settled.'; END IF;
    SELECT COUNT(*) INTO v_joined_players FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id;
    IF v_joined_players<>v_required_players THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The battle does not have every required participant.'; END IF;
    SELECT COUNT(*)*v_joined_players INTO v_expected FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_rolls FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
    IF v_rolls<>v_expected THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The verified server roll set is incomplete.'; END IF;

    UPDATE CaseOpeningBattleParticipants participant
    SET TotalValue=(SELECT COALESCE(SUM(roll.LockedValue),0) FROM CaseOpeningBattleRolls roll WHERE roll.BattleId=p_battle_id AND roll.OriginalOwnerUserId=participant.UserId)
    WHERE participant.BattleId=p_battle_id;
    SELECT participant.UserId INTO v_winner
    FROM CaseOpeningBattleParticipants participant
    WHERE participant.BattleId=p_battle_id
    ORDER BY participant.TotalValue DESC,
        (SELECT COALESCE(MAX(roll.LockedValue),0) FROM CaseOpeningBattleRolls roll WHERE roll.BattleId=p_battle_id AND roll.OriginalOwnerUserId=participant.UserId) DESC,
        (SELECT COALESCE(MIN(roll.RoundNumber),9999) FROM CaseOpeningBattleRolls roll WHERE roll.BattleId=p_battle_id AND roll.OriginalOwnerUserId=participant.UserId AND roll.LockedValue=(SELECT MAX(maxRoll.LockedValue) FROM CaseOpeningBattleRolls maxRoll WHERE maxRoll.BattleId=p_battle_id AND maxRoll.OriginalOwnerUserId=participant.UserId)) ASC,
        participant.Seat ASC LIMIT 1;
    UPDATE CaseOpeningBattleRolls SET AwardedToUserId=v_winner WHERE BattleId=p_battle_id;
    INSERT INTO CaseOpeningBattlePulls(BattlePullId,BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,IsSoldForSplit)
    SELECT UUID(),BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,0 FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;

    IF v_winner IN ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002') THEN
        UPDATE CaseOpeningBattleParticipants SET AwardedValue=0 WHERE BattleId=p_battle_id;
        UPDATE CaseOpeningBattleBotStats SET BattlesWon=BattlesWon+1,SkinsDiscarded=SkinsDiscarded+v_rolls,
            ValueDiscarded=ValueDiscarded+(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id),UpdatedUtc=UTC_TIMESTAMP(6) WHERE StatsId=1;
        UPDATE CaseOpeningBattleOverflowReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
        UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
        UPDATE CaseOpeningBattleInvitations SET Status=IF(Status='accepted','accepted','cancelled'),RespondedUtc=COALESCE(RespondedUtc,UTC_TIMESTAMP(6)) WHERE BattleId=p_battle_id;
        UPDATE CaseOpeningBattles SET Status='settled',WinningUserId=v_winner,WinnerTieBreak='total, highest single pull, earliest locked round, seat',SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
        COMMIT;
        LEAVE settle;
    END IF;

    SELECT GREATEST(capacity.BaseCapacity+COALESCE((SELECT SUM(AddedSlots) FROM CaseOpeningStorageContainers WHERE UserId=v_winner),0)
        +COALESCE(upgrades.BonusInventorySlots,0)-(SELECT COUNT(*) FROM CaseOpeningHistory WHERE UserId=v_winner)
        -(SELECT COALESCE(SUM(Quantity),0) FROM CaseOpeningOwnedCases WHERE UserId=v_winner),0)
    INTO v_capacity FROM CaseOpeningInventoryCapacity capacity
    LEFT JOIN CaseOpeningUserInventoryUpgrades upgrades ON upgrades.UserId=capacity.UserId WHERE capacity.UserId=v_winner FOR UPDATE;
    SET v_deliver=LEAST(v_rolls,COALESCE(v_capacity,0));
    SET v_overflow=v_rolls-v_deliver;
    IF v_overflow>(SELECT ReservedSlots FROM CaseOpeningBattleOverflowReservations WHERE BattleId=p_battle_id AND UserId=v_winner AND ReleasedUtc IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The winner no longer has sufficient reserved Battle Overflow.';
    END IF;
    SET v_allocation_reason=IF(v_mode='ffa-3','ffa-3 winner','duel winner');
    INSERT INTO CaseOpeningBattleSettlements(BattleSettlementId,BattleId,BattleRollId,RecipientUserId,Delivery,LockedValue,AllocationReason,CreatedUtc)
    SELECT UUID(),p_battle_id,ordered.BattleRollId,v_winner,IF(ordered.RowNumber<=v_deliver,'inventory','overflow'),ordered.LockedValue,v_allocation_reason,UTC_TIMESTAMP(6)
    FROM (SELECT BattleRollId,LockedValue,ROW_NUMBER() OVER (ORDER BY RoundNumber,BattleRollId) RowNumber FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id) ordered;
    INSERT INTO CaseOpeningHistory(OpeningId,BattleId,UserId,OriginalOwnerUserId,CaseKey,SourceItemId,ItemName,MarketHashName,ImageUrl,Description,WeaponName,PatternName,PaintIndex,Phase,RarityKey,RarityName,RarityColor,Wear,IsStatTrak,IsRareSpecial,SupportsStatTrak,EstimatedPrice,OpenedUtc)
    SELECT roll.OpeningId,p_battle_id,v_winner,roll.OriginalOwnerUserId,roll.CaseKey,roll.SourceItemId,roll.ItemName,roll.MarketHashName,roll.ImageUrl,'','','','','',roll.RarityKey,roll.RarityName,roll.RarityColor,roll.Wear,roll.IsStatTrak,roll.IsRareSpecial,roll.SupportsStatTrak,roll.LockedValue,UTC_TIMESTAMP(6)
    FROM CaseOpeningBattleRolls roll INNER JOIN CaseOpeningBattleSettlements settlement ON settlement.BattleRollId=roll.BattleRollId
    WHERE settlement.BattleId=p_battle_id AND settlement.Delivery='inventory';
    UPDATE CaseOpeningBattleParticipants participant
    SET AwardedValue=IF(participant.UserId=v_winner,(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id),0)
    WHERE participant.BattleId=p_battle_id;
    UPDATE CaseOpeningBattleOverflow overflowRow INNER JOIN CaseOpeningBattleOverflowReservations reservation ON reservation.UserId=overflowRow.UserId
    SET overflowRow.OccupiedSlots=overflowRow.OccupiedSlots+IF(reservation.UserId=v_winner,v_overflow,0),overflowRow.UpdatedUtc=UTC_TIMESTAMP(6),
        reservation.OccupiedSlots=IF(reservation.UserId=v_winner,v_overflow,0),reservation.ReleasedUtc=UTC_TIMESTAMP(6)
    WHERE reservation.BattleId=p_battle_id AND reservation.ReleasedUtc IS NULL;
    UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
    UPDATE CaseOpeningBattleInvitations SET Status=IF(Status='accepted','accepted','cancelled'),RespondedUtc=COALESCE(RespondedUtc,UTC_TIMESTAMP(6)) WHERE BattleId=p_battle_id;
    UPDATE CaseOpeningBattles SET Status='settled',WinningUserId=v_winner,WinnerTieBreak='total, highest single pull, earliest locked round, seat',SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
    COMMIT;
END//

DELIMITER ;

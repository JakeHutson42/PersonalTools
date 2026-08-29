-- System-owned opponent. The bot is never an authenticated account and never receives inventory.
USE PersonalTools;
SET @battle_bot_id = '00000000-0000-0000-0000-000000000001';
INSERT IGNORE INTO Users(UserId,Email,DisplayName,PasswordHash,IsActive,UserRole,CreatedUtc) VALUES(@battle_bot_id,'battle-bot@system.invalid','Battle Bot','SYSTEM-ACCOUNT',0,1,UTC_TIMESTAMP(6));
CREATE TABLE IF NOT EXISTS CaseOpeningBattleBotSettings(SettingsId TINYINT NOT NULL PRIMARY KEY DEFAULT 1,CaseBattlesEnabled TINYINT(1) NOT NULL DEFAULT 0,Enabled TINYINT(1) NOT NULL DEFAULT 0,UpdatedUtc DATETIME(6) NOT NULL) ENGINE=InnoDB;
INSERT IGNORE INTO CaseOpeningBattleBotSettings(SettingsId,CaseBattlesEnabled,Enabled,UpdatedUtc) VALUES(1,0,0,UTC_TIMESTAMP(6));
CREATE TABLE IF NOT EXISTS CaseOpeningBattleBotStats(StatsId TINYINT NOT NULL PRIMARY KEY DEFAULT 1,BattlesAttempted INT UNSIGNED NOT NULL DEFAULT 0,BattlesWon INT UNSIGNED NOT NULL DEFAULT 0,SkinsDiscarded INT UNSIGNED NOT NULL DEFAULT 0,ValueDiscarded DECIMAL(12,2) NOT NULL DEFAULT 0,UpdatedUtc DATETIME(6) NOT NULL) ENGINE=InnoDB;
INSERT IGNORE INTO CaseOpeningBattleBotStats(StatsId,UpdatedUtc) VALUES(1,UTC_TIMESTAMP(6));
CREATE TABLE IF NOT EXISTS CaseOpeningBattleBotAcquisitions(BattleId CHAR(36) NOT NULL,CaseKey VARCHAR(80) NOT NULL,Quantity INT UNSIGNED NOT NULL,PRIMARY KEY(BattleId,CaseKey),CONSTRAINT FK_CaseOpeningBattleBotAcquisitions_Battle FOREIGN KEY(BattleId) REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE) ENGINE=InnoDB;
DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_bot_status_get//
CREATE PROCEDURE sp_case_battles_bot_status_get() BEGIN SELECT settings.CaseBattlesEnabled,settings.Enabled,stats.BattlesAttempted,stats.BattlesWon,stats.SkinsDiscarded,stats.ValueDiscarded FROM CaseOpeningBattleBotSettings settings INNER JOIN CaseOpeningBattleBotStats stats ON stats.StatsId=1 WHERE settings.SettingsId=1; END//
DROP PROCEDURE IF EXISTS sp_case_battles_feature_enabled_set//
CREATE PROCEDURE sp_case_battles_feature_enabled_set(IN p_enabled TINYINT) BEGIN UPDATE CaseOpeningBattleBotSettings SET CaseBattlesEnabled=IF(p_enabled<>0,1,0),UpdatedUtc=UTC_TIMESTAMP(6) WHERE SettingsId=1; END//
DROP PROCEDURE IF EXISTS sp_case_battles_bot_enabled_set//
CREATE PROCEDURE sp_case_battles_bot_enabled_set(IN p_enabled TINYINT) BEGIN UPDATE CaseOpeningBattleBotSettings SET Enabled=IF(p_enabled<>0,1,0),UpdatedUtc=UTC_TIMESTAMP(6) WHERE SettingsId=1; END//
DROP PROCEDURE IF EXISTS sp_case_battles_invitable_users_get//
CREATE PROCEDURE sp_case_battles_invitable_users_get(IN p_user_id CHAR(36)) BEGIN SELECT UserId,DisplayName FROM Users WHERE IsActive=1 AND BINARY UserId<>BINARY p_user_id AND BINARY UserId<>BINARY '00000000-0000-0000-0000-000000000001' ORDER BY DisplayName; END//
DROP PROCEDURE IF EXISTS sp_case_battles_bot_join//
CREATE PROCEDURE sp_case_battles_bot_join(IN p_battle_id CHAR(36))
BEGIN
 IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattleBotSettings WHERE SettingsId=1 AND Enabled=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot is disabled.'; END IF;
 IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting') OR (SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id)<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle cannot be joined by Battle Bot.'; END IF;
 INSERT INTO CaseOpeningBattleBotAcquisitions(BattleId,CaseKey,Quantity) SELECT p_battle_id,CaseKey,COUNT(*) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey;
 INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) SELECT '00000000-0000-0000-0000-000000000001',CaseKey,COUNT(*),UTC_TIMESTAMP(6) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey ON DUPLICATE KEY UPDATE Quantity=Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
 CALL sp_case_battles_join(p_battle_id,'00000000-0000-0000-0000-000000000001');
 UPDATE CaseOpeningBattleParticipants SET IsReady=1 WHERE BattleId=p_battle_id AND UserId='00000000-0000-0000-0000-000000000001';
 UPDATE CaseOpeningBattleBotStats SET BattlesAttempted=BattlesAttempted+1,UpdatedUtc=UTC_TIMESTAMP(6) WHERE StatsId=1;
END//
DROP PROCEDURE IF EXISTS sp_case_battles_settle_staged//
CREATE PROCEDURE sp_case_battles_settle_staged(IN p_battle_id CHAR(36))
settle: BEGIN
 DECLARE v_winner CHAR(36) DEFAULT NULL; DECLARE v_rolls INT DEFAULT 0; DECLARE v_expected INT DEFAULT 0; DECLARE v_capacity INT DEFAULT 0; DECLARE v_deliver INT DEFAULT 0; DECLARE v_overflow INT DEFAULT 0; DECLARE v_status VARCHAR(16) DEFAULT ''; DECLARE v_mode VARCHAR(16) DEFAULT ''; DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END; START TRANSACTION;
 SELECT Status,Mode INTO v_status,v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE; IF v_status<>'opening' OR v_mode<>'duel' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Only an opening duel can be settled.'; END IF;
 SELECT COUNT(*)*(SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id) INTO v_expected FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id; SELECT COUNT(*) INTO v_rolls FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id; IF v_rolls<>v_expected THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The verified server roll set is incomplete.'; END IF;
 UPDATE CaseOpeningBattleParticipants p SET TotalValue=(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls r WHERE r.BattleId=p_battle_id AND r.OriginalOwnerUserId=p.UserId) WHERE p.BattleId=p_battle_id;
 SELECT p.UserId INTO v_winner FROM CaseOpeningBattleParticipants p WHERE p.BattleId=p_battle_id ORDER BY p.TotalValue DESC,(SELECT COALESCE(MAX(LockedValue),0) FROM CaseOpeningBattleRolls r WHERE r.BattleId=p_battle_id AND r.OriginalOwnerUserId=p.UserId) DESC,(SELECT COALESCE(MIN(RoundNumber),9999) FROM CaseOpeningBattleRolls r WHERE r.BattleId=p_battle_id AND r.OriginalOwnerUserId=p.UserId AND r.LockedValue=(SELECT MAX(LockedValue) FROM CaseOpeningBattleRolls maxRoll WHERE maxRoll.BattleId=p_battle_id AND maxRoll.OriginalOwnerUserId=p.UserId)) ASC,p.Seat ASC LIMIT 1;
 UPDATE CaseOpeningBattleRolls SET AwardedToUserId=v_winner WHERE BattleId=p_battle_id;
 INSERT INTO CaseOpeningBattlePulls(BattlePullId,BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,IsSoldForSplit) SELECT UUID(),BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,0 FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
 IF BINARY v_winner=BINARY '00000000-0000-0000-0000-000000000001' THEN
   UPDATE CaseOpeningBattleParticipants SET AwardedValue=0 WHERE BattleId=p_battle_id;
   UPDATE CaseOpeningBattleBotStats SET BattlesWon=BattlesWon+1,SkinsDiscarded=SkinsDiscarded+v_rolls,ValueDiscarded=ValueDiscarded+(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id),UpdatedUtc=UTC_TIMESTAMP(6) WHERE StatsId=1;
   UPDATE CaseOpeningBattleOverflowReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL; UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
   UPDATE CaseOpeningBattles SET Status='settled',WinningUserId=v_winner,WinnerTieBreak='total, highest single pull, earliest locked round, seat',SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id; COMMIT; LEAVE settle;
 END IF;
 SELECT GREATEST(capacity.BaseCapacity+COALESCE((SELECT SUM(AddedSlots) FROM CaseOpeningStorageContainers WHERE UserId=v_winner),0)+COALESCE(upgrades.BonusInventorySlots,0)-(SELECT COUNT(*) FROM CaseOpeningHistory WHERE UserId=v_winner)-(SELECT COALESCE(SUM(Quantity),0) FROM CaseOpeningOwnedCases WHERE UserId=v_winner),0) INTO v_capacity FROM CaseOpeningInventoryCapacity capacity LEFT JOIN CaseOpeningUserInventoryUpgrades upgrades ON upgrades.UserId=capacity.UserId WHERE capacity.UserId=v_winner FOR UPDATE;
 SET v_deliver=LEAST(v_rolls,COALESCE(v_capacity,0)); SET v_overflow=v_rolls-v_deliver;
 IF v_overflow>(SELECT ReservedSlots FROM CaseOpeningBattleOverflowReservations WHERE BattleId=p_battle_id AND UserId=v_winner AND ReleasedUtc IS NULL) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The winner no longer has sufficient reserved Battle Overflow.'; END IF;
 INSERT INTO CaseOpeningBattleSettlements(BattleSettlementId,BattleId,BattleRollId,RecipientUserId,Delivery,LockedValue,AllocationReason,CreatedUtc) SELECT UUID(),p_battle_id,ordered.BattleRollId,v_winner,IF(ordered.RowNumber<=v_deliver,'inventory','overflow'),ordered.LockedValue,'duel winner',UTC_TIMESTAMP(6) FROM (SELECT BattleRollId,LockedValue,ROW_NUMBER() OVER (ORDER BY RoundNumber,BattleRollId) RowNumber FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id) ordered;
 INSERT INTO CaseOpeningHistory(OpeningId,BattleId,UserId,OriginalOwnerUserId,CaseKey,SourceItemId,ItemName,MarketHashName,ImageUrl,Description,WeaponName,PatternName,PaintIndex,Phase,RarityKey,RarityName,RarityColor,Wear,IsStatTrak,IsRareSpecial,SupportsStatTrak,EstimatedPrice,OpenedUtc) SELECT roll.OpeningId,p_battle_id,v_winner,roll.OriginalOwnerUserId,roll.CaseKey,roll.SourceItemId,roll.ItemName,roll.MarketHashName,roll.ImageUrl,'','','','','',roll.RarityKey,roll.RarityName,roll.RarityColor,roll.Wear,roll.IsStatTrak,roll.IsRareSpecial,roll.SupportsStatTrak,roll.LockedValue,UTC_TIMESTAMP(6) FROM CaseOpeningBattleRolls roll INNER JOIN CaseOpeningBattleSettlements s ON s.BattleRollId=roll.BattleRollId WHERE s.BattleId=p_battle_id AND s.Delivery='inventory';
 UPDATE CaseOpeningBattleParticipants p SET AwardedValue=IF(p.UserId=v_winner,(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id),0) WHERE p.BattleId=p_battle_id;
 UPDATE CaseOpeningBattleOverflow o INNER JOIN CaseOpeningBattleOverflowReservations r ON r.UserId=o.UserId SET o.OccupiedSlots=o.OccupiedSlots+IF(r.UserId=v_winner,v_overflow,0),o.UpdatedUtc=UTC_TIMESTAMP(6),r.OccupiedSlots=IF(r.UserId=v_winner,v_overflow,0),r.ReleasedUtc=UTC_TIMESTAMP(6) WHERE r.BattleId=p_battle_id AND r.ReleasedUtc IS NULL; UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL; UPDATE CaseOpeningBattles SET Status='settled',WinningUserId=v_winner,WinnerTieBreak='total, highest single pull, earliest locked round, seat',SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id; COMMIT;
END//
DELIMITER ;

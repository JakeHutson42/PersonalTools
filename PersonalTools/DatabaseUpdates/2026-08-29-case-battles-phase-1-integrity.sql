-- Phase 1: battle escrow and settlement integrity.
-- Run after 2026-08-29-case-battles-foundation.sql. Case copies are removed from a
-- participant's normal stock while the battle is unresolved, so normal opening, bots and
-- discarding cannot consume an entered case. Nothing in this script trusts a browser winner.
USE PersonalTools;

ALTER TABLE CaseOpeningBattles
    ADD COLUMN IF NOT EXISTS LockedUtc DATETIME(6) NULL AFTER StartedUtc,
    ADD COLUMN IF NOT EXISTS CancelledUtc DATETIME(6) NULL AFTER SettledUtc,
    ADD COLUMN IF NOT EXISTS WinnerTieBreak VARCHAR(80) NULL AFTER WinningTeam;

ALTER TABLE CaseOpeningHistory
    ADD COLUMN IF NOT EXISTS BattleId CHAR(36) NULL AFTER OpeningId,
    ADD COLUMN IF NOT EXISTS OriginalOwnerUserId CHAR(36) NULL AFTER UserId,
    ADD KEY IF NOT EXISTS IX_CaseOpeningHistory_Battle (BattleId);

CREATE TABLE IF NOT EXISTS CaseOpeningBattleCaseReservations
(
    BattleId CHAR(36) NOT NULL,
    UserId CHAR(36) NOT NULL,
    CaseKey VARCHAR(80) NOT NULL,
    Quantity INT UNSIGNED NOT NULL,
    ReservedUtc DATETIME(6) NOT NULL,
    ReleasedUtc DATETIME(6) NULL,
    PRIMARY KEY (BattleId, UserId, CaseKey),
    KEY IX_CaseOpeningBattleCaseReservations_User (UserId, ReleasedUtc),
    CONSTRAINT FK_CaseOpeningBattleCaseReservations_Battle FOREIGN KEY (BattleId)
        REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE,
    CONSTRAINT FK_CaseOpeningBattleCaseReservations_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CaseOpeningBattleOverflow
(
    UserId CHAR(36) NOT NULL,
    SlotAllowance SMALLINT UNSIGNED NOT NULL DEFAULT 100,
    OccupiedSlots SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    UpdatedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY (UserId),
    CONSTRAINT FK_CaseOpeningBattleOverflow_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CaseOpeningBattleOverflowReservations
(
    BattleId CHAR(36) NOT NULL,
    UserId CHAR(36) NOT NULL,
    ReservedSlots SMALLINT UNSIGNED NOT NULL,
    OccupiedSlots SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    ReservedUtc DATETIME(6) NOT NULL,
    ReleasedUtc DATETIME(6) NULL,
    PRIMARY KEY (BattleId, UserId),
    KEY IX_CaseOpeningBattleOverflowReservations_User (UserId, ReleasedUtc),
    CONSTRAINT FK_CaseOpeningBattleOverflowReservations_Battle FOREIGN KEY (BattleId)
        REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE,
    CONSTRAINT FK_CaseOpeningBattleOverflowReservations_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CaseOpeningBattleSettlements
(
    BattleSettlementId CHAR(36) NOT NULL,
    BattleId CHAR(36) NOT NULL,
    BattleRollId CHAR(36) NOT NULL,
    RecipientUserId CHAR(36) NOT NULL,
    Delivery VARCHAR(16) NOT NULL,
    LockedValue DECIMAL(12,2) NOT NULL,
    SaleValue DECIMAL(12,2) NULL,
    AllocationReason VARCHAR(80) NOT NULL,
    CreatedUtc DATETIME(6) NOT NULL,
    ClaimedUtc DATETIME(6) NULL,
    PRIMARY KEY (BattleSettlementId),
    UNIQUE KEY UX_CaseOpeningBattleSettlements_Roll (BattleRollId),
    KEY IX_CaseOpeningBattleSettlements_Recipient (RecipientUserId, Delivery, ClaimedUtc),
    CONSTRAINT FK_CaseOpeningBattleSettlements_Battle FOREIGN KEY (BattleId)
        REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE,
    CONSTRAINT FK_CaseOpeningBattleSettlements_Roll FOREIGN KEY (BattleRollId)
        REFERENCES CaseOpeningBattleRolls(BattleRollId) ON DELETE CASCADE,
    CONSTRAINT FK_CaseOpeningBattleSettlements_Recipient FOREIGN KEY (RecipientUserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

DELIMITER //

-- The caller already owns a battle row lock. Returning escrow here is idempotent: ReleasedUtc
-- is written in the same transaction as the refund and prevents an expiry job from refunding twice.
DROP PROCEDURE IF EXISTS sp_case_battles_reservations_release//
CREATE PROCEDURE sp_case_battles_reservations_release(IN p_battle_id CHAR(36))
BEGIN
    INSERT INTO CaseOpeningOwnedCases(UserId, CaseKey, Quantity, UpdatedUtc)
    SELECT UserId, CaseKey, Quantity, UTC_TIMESTAMP(6)
    FROM CaseOpeningBattleCaseReservations
    WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL
    ON DUPLICATE KEY UPDATE Quantity=Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
    UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6)
    WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
    UPDATE CaseOpeningBattleOverflowReservations SET ReleasedUtc=UTC_TIMESTAMP(6)
    WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_create//
CREATE PROCEDURE sp_case_battles_create(IN p_battle_id CHAR(36), IN p_user_id CHAR(36), IN p_mode VARCHAR(16), IN p_case_keys JSON)
BEGIN
    DECLARE v_case_count INT DEFAULT 0;
    DECLARE v_distinct_cases INT DEFAULT 0;
    DECLARE v_required_players INT DEFAULT 0;
    DECLARE v_locked_rows INT DEFAULT 0;
    DECLARE v_reservation_slots INT DEFAULT 0;
    DECLARE v_allowance INT DEFAULT 0;
    DECLARE v_occupied INT DEFAULT 0;
    DECLARE v_reserved INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SET v_required_players=CASE p_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 WHEN 'ffa-4' THEN 4 WHEN 'teams-2v2' THEN 4 ELSE 0 END;
    SET v_case_count=COALESCE(JSON_LENGTH(p_case_keys),0);
    IF v_required_players=0 OR v_case_count<1 OR v_case_count>20 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose a valid battle mode and between 1 and 20 cases.'; END IF;
    SELECT COUNT(*) INTO v_distinct_cases FROM (SELECT CaseKey FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) required GROUP BY CaseKey) cases;
    SELECT COUNT(*) INTO v_locked_rows FROM CaseOpeningOwnedCases owned
    INNER JOIN (SELECT CaseKey,COUNT(*) Quantity FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) required GROUP BY CaseKey) required ON required.CaseKey=owned.CaseKey
    WHERE owned.UserId=p_user_id AND owned.Quantity>=required.Quantity FOR UPDATE;
    IF v_locked_rows<>v_distinct_cases THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You need to own every selected case before creating a battle.'; END IF;
    INSERT INTO CaseOpeningBattleOverflow(UserId,SlotAllowance,OccupiedSlots,UpdatedUtc) VALUES(p_user_id,100,0,UTC_TIMESTAMP(6)) ON DUPLICATE KEY UPDATE UpdatedUtc=UpdatedUtc;
    SELECT SlotAllowance,OccupiedSlots INTO v_allowance,v_occupied FROM CaseOpeningBattleOverflow WHERE UserId=p_user_id FOR UPDATE;
    SELECT COALESCE(SUM(ReservedSlots),0) INTO v_reserved FROM CaseOpeningBattleOverflowReservations WHERE UserId=p_user_id AND ReleasedUtc IS NULL FOR UPDATE;
    SET v_reservation_slots=v_case_count*v_required_players;
    IF v_occupied+v_reserved+v_reservation_slots>v_allowance THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Your Battle Overflow allowance does not have enough space for this battle.'; END IF;
    UPDATE CaseOpeningOwnedCases owned INNER JOIN (SELECT CaseKey,COUNT(*) Quantity FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) required GROUP BY CaseKey) required ON required.CaseKey=owned.CaseKey
    SET owned.Quantity=owned.Quantity-required.Quantity, owned.UpdatedUtc=UTC_TIMESTAMP(6) WHERE owned.UserId=p_user_id AND owned.Quantity>=required.Quantity;
    IF ROW_COUNT()<>v_distinct_cases THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Your case ownership changed before the battle could be created.'; END IF;
    INSERT INTO CaseOpeningBattles(BattleId,CreatorUserId,Mode,Status,CreatedUtc,ExpiresUtc) VALUES(p_battle_id,p_user_id,p_mode,'waiting',UTC_TIMESTAMP(6),DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE));
    INSERT INTO CaseOpeningBattleCases(BattleId,RoundNumber,CaseKey) SELECT p_battle_id,ord-1,CaseKey FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(ord FOR ORDINALITY,CaseKey VARCHAR(80) PATH '$')) required;
    INSERT INTO CaseOpeningBattleParticipants(BattleId,UserId,Seat,Team,OverflowReservedSlots,JoinedUtc) VALUES(p_battle_id,p_user_id,1,IF(p_mode='teams-2v2',1,0),v_reservation_slots,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningBattleCaseReservations(BattleId,UserId,CaseKey,Quantity,ReservedUtc) SELECT p_battle_id,p_user_id,CaseKey,COUNT(*),UTC_TIMESTAMP(6) FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) required GROUP BY CaseKey;
    INSERT INTO CaseOpeningBattleOverflowReservations(BattleId,UserId,ReservedSlots,ReservedUtc) VALUES(p_battle_id,p_user_id,v_reservation_slots,UTC_TIMESTAMP(6));
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_join//
CREATE PROCEDURE sp_case_battles_join(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT '';
    DECLARE v_required_players INT DEFAULT 0;
    DECLARE v_joined INT DEFAULT 0;
    DECLARE v_case_count INT DEFAULT 0;
    DECLARE v_distinct_cases INT DEFAULT 0;
    DECLARE v_locked_rows INT DEFAULT 0;
    DECLARE v_reservation_slots INT DEFAULT 0;
    DECLARE v_allowance INT DEFAULT 0;
    DECLARE v_occupied INT DEFAULT 0;
    DECLARE v_reserved INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Mode INTO v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting' AND ExpiresUtc>UTC_TIMESTAMP(6) FOR UPDATE;
    IF v_mode='' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle is no longer accepting players.'; END IF;
    IF EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId=p_user_id) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You have already joined this battle.'; END IF;
    SET v_required_players=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 ELSE 4 END;
    SELECT COUNT(*) INTO v_joined FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id;
    IF v_joined>=v_required_players THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle is full.'; END IF;
    SELECT COUNT(*),COUNT(DISTINCT CaseKey) INTO v_case_count,v_distinct_cases FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_locked_rows FROM CaseOpeningOwnedCases owned INNER JOIN (SELECT CaseKey,COUNT(*) Quantity FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey) required ON required.CaseKey=owned.CaseKey
    WHERE owned.UserId=p_user_id AND owned.Quantity>=required.Quantity FOR UPDATE;
    IF v_locked_rows<>v_distinct_cases THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You need to own every selected case before joining.'; END IF;
    INSERT INTO CaseOpeningBattleOverflow(UserId,SlotAllowance,OccupiedSlots,UpdatedUtc) VALUES(p_user_id,100,0,UTC_TIMESTAMP(6)) ON DUPLICATE KEY UPDATE UpdatedUtc=UpdatedUtc;
    SELECT SlotAllowance,OccupiedSlots INTO v_allowance,v_occupied FROM CaseOpeningBattleOverflow WHERE UserId=p_user_id FOR UPDATE;
    SELECT COALESCE(SUM(ReservedSlots),0) INTO v_reserved FROM CaseOpeningBattleOverflowReservations WHERE UserId=p_user_id AND ReleasedUtc IS NULL FOR UPDATE;
    SET v_reservation_slots=v_case_count*v_required_players;
    IF v_occupied+v_reserved+v_reservation_slots>v_allowance THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Your Battle Overflow allowance does not have enough space for this battle.'; END IF;
    UPDATE CaseOpeningOwnedCases owned INNER JOIN (SELECT CaseKey,COUNT(*) Quantity FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey) required ON required.CaseKey=owned.CaseKey
    SET owned.Quantity=owned.Quantity-required.Quantity, owned.UpdatedUtc=UTC_TIMESTAMP(6) WHERE owned.UserId=p_user_id AND owned.Quantity>=required.Quantity;
    IF ROW_COUNT()<>v_distinct_cases THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Your case ownership changed before the battle could be joined.'; END IF;
    INSERT INTO CaseOpeningBattleParticipants(BattleId,UserId,Seat,Team,OverflowReservedSlots,JoinedUtc) VALUES(p_battle_id,p_user_id,v_joined+1,IF(v_mode='teams-2v2',IF(v_joined<2,1,2),0),v_reservation_slots,UTC_TIMESTAMP(6));
    INSERT INTO CaseOpeningBattleCaseReservations(BattleId,UserId,CaseKey,Quantity,ReservedUtc) SELECT p_battle_id,p_user_id,CaseKey,COUNT(*),UTC_TIMESTAMP(6) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey;
    INSERT INTO CaseOpeningBattleOverflowReservations(BattleId,UserId,ReservedSlots,ReservedUtc) VALUES(p_battle_id,p_user_id,v_reservation_slots,UTC_TIMESTAMP(6));
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_cancel//
CREATE PROCEDURE sp_case_battles_cancel(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    DECLARE v_creator CHAR(36) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT CreatorUserId INTO v_creator FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting' FOR UPDATE;
    IF v_creator IS NULL OR BINARY v_creator<>BINARY p_user_id THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Only the creator can cancel a waiting battle.'; END IF;
    UPDATE CaseOpeningBattles SET Status='cancelled',CancelledUtc=UTC_TIMESTAMP(6),SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
    CALL sp_case_battles_reservations_release(p_battle_id);
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_leave//
CREATE PROCEDURE sp_case_battles_leave(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    DECLARE v_creator CHAR(36) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT CreatorUserId INTO v_creator FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting' FOR UPDATE;
    IF v_creator IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle can no longer be left.'; END IF;
    IF BINARY v_creator=BINARY p_user_id THEN
        UPDATE CaseOpeningBattles SET Status='cancelled',CancelledUtc=UTC_TIMESTAMP(6),SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
        CALL sp_case_battles_reservations_release(p_battle_id);
    ELSE
        IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId=p_user_id) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You are not in this battle.'; END IF;
        INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) SELECT UserId,CaseKey,Quantity,UTC_TIMESTAMP(6) FROM CaseOpeningBattleCaseReservations WHERE BattleId=p_battle_id AND UserId=p_user_id AND ReleasedUtc IS NULL ON DUPLICATE KEY UPDATE Quantity=Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
        UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND UserId=p_user_id AND ReleasedUtc IS NULL;
        UPDATE CaseOpeningBattleOverflowReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND UserId=p_user_id AND ReleasedUtc IS NULL;
        DELETE FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId=p_user_id;
    END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_expire//
CREATE PROCEDURE sp_case_battles_expire(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_status VARCHAR(16) DEFAULT '';
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Status INTO v_status FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE;
    IF v_status='waiting' AND EXISTS(SELECT 1 FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND ExpiresUtc<=UTC_TIMESTAMP(6)) THEN
        UPDATE CaseOpeningBattles SET Status='cancelled',CancelledUtc=UTC_TIMESTAMP(6),SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
        CALL sp_case_battles_reservations_release(p_battle_id);
    END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_ready_set//
CREATE PROCEDURE sp_case_battles_ready_set(IN p_battle_id CHAR(36), IN p_user_id CHAR(36), IN p_is_ready TINYINT)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT BattleId FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting' AND ExpiresUtc>UTC_TIMESTAMP(6) FOR UPDATE;
    UPDATE CaseOpeningBattleParticipants SET IsReady=IF(p_is_ready<>0,1,0) WHERE BattleId=p_battle_id AND UserId=p_user_id;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle can no longer be readied.'; END IF;
    COMMIT;
END//

-- Starting is deliberately separate from readying. It is an idempotent server-only boundary
-- that freezes the active market snapshot after every seat is present and ready.
DROP PROCEDURE IF EXISTS sp_case_battles_start//
CREATE PROCEDURE sp_case_battles_start(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16) DEFAULT '';
    DECLARE v_required INT DEFAULT 0;
    DECLARE v_joined INT DEFAULT 0;
    DECLARE v_ready INT DEFAULT 0;
    DECLARE v_snapshot CHAR(36) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Mode INTO v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting' AND ExpiresUtc>UTC_TIMESTAMP(6) FOR UPDATE;
    IF v_mode='' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle cannot be started.'; END IF;
    SET v_required=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 ELSE 4 END;
    SELECT COUNT(*),COALESCE(SUM(IsReady),0) INTO v_joined,v_ready FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id;
    IF v_joined<>v_required OR v_ready<>v_required THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Every participant must join and ready before the battle starts.'; END IF;
    SELECT PriceSnapshotId INTO v_snapshot FROM CaseOpeningPriceSnapshots WHERE IsActive=1 ORDER BY ImportedUtc DESC LIMIT 1;
    IF v_snapshot IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='An active price snapshot is required before starting a battle.'; END IF;
    UPDATE CaseOpeningBattles SET Status='opening',PriceSnapshotId=v_snapshot,StartedUtc=UTC_TIMESTAMP(6),LockedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
    COMMIT;
END//

-- Phase 2 supplies the server-generated roll JSON. This staging boundary validates each value
-- against the frozen snapshot and keeps a completed prior set intact on retry.
DROP PROCEDURE IF EXISTS sp_case_battles_execution_plan_get//
CREATE PROCEDURE sp_case_battles_execution_plan_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT participant.UserId,participant.Seat,battleCase.RoundNumber,battleCase.CaseKey,battle.Status,battle.Mode,battle.PriceSnapshotId
    FROM CaseOpeningBattles battle
    INNER JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId
    INNER JOIN CaseOpeningBattleCases battleCase ON battleCase.BattleId=battle.BattleId
    WHERE battle.BattleId=p_battle_id
      AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=battle.BattleId AND me.UserId=p_user_id)
    ORDER BY battleCase.RoundNumber,participant.Seat;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_locked_market_hashes_get//
CREATE PROCEDURE sp_case_battles_locked_market_hashes_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT price.MarketHashName
    FROM CaseOpeningBattles battle
    INNER JOIN CaseOpeningPriceSnapshotItems price ON price.PriceSnapshotId=battle.PriceSnapshotId
    WHERE battle.BattleId=p_battle_id
      AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=battle.BattleId AND me.UserId=p_user_id);
END//

DROP PROCEDURE IF EXISTS sp_case_battles_participants_get//
CREATE PROCEDURE sp_case_battles_participants_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT participant.UserId,user.DisplayName,participant.Seat,participant.Team,participant.IsReady,
        participant.TotalValue,participant.OverflowReservedSlots
    FROM CaseOpeningBattleParticipants participant
    INNER JOIN Users user ON user.UserId=participant.UserId
    WHERE participant.BattleId=p_battle_id
      AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=p_battle_id AND me.UserId=p_user_id)
    ORDER BY participant.Seat;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_pulls_get//
CREATE PROCEDURE sp_case_battles_pulls_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT roll.BattleRollId,roll.OriginalOwnerUserId,user.DisplayName OriginalOwnerDisplayName,roll.RoundNumber,
        roll.ItemName,roll.ImageUrl,roll.RarityColor,roll.LockedValue,settlement.Delivery
    FROM CaseOpeningBattleRolls roll
    INNER JOIN Users user ON user.UserId=roll.OriginalOwnerUserId
    LEFT JOIN CaseOpeningBattleSettlements settlement ON settlement.BattleRollId=roll.BattleRollId
    WHERE roll.BattleId=p_battle_id
      AND EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=p_battle_id AND me.UserId=p_user_id)
    ORDER BY roll.RoundNumber,roll.BattleRollId;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_rolls_stage//
CREATE PROCEDURE sp_case_battles_rolls_stage(IN p_battle_id CHAR(36), IN p_rolls JSON)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_staged INT DEFAULT 0;
    DECLARE v_input INT DEFAULT 0;
    DECLARE v_invalid INT DEFAULT 0;
    DECLARE v_status VARCHAR(16) DEFAULT '';
    DECLARE v_snapshot CHAR(36) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT COUNT(*)*(SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id) INTO v_expected FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_staged FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
    IF v_staged=v_expected AND v_expected>0 THEN
        COMMIT;
    ELSE
        IF v_staged<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='A partial battle roll set cannot be replaced.'; END IF;
        SELECT Status,PriceSnapshotId INTO v_status,v_snapshot FROM CaseOpeningBattles
        WHERE BattleId=p_battle_id FOR UPDATE;
        IF v_status<>'opening' OR v_snapshot IS NULL THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle is not ready for server rolls.'; END IF;
        SET v_input=COALESCE(JSON_LENGTH(p_rolls),0);
        IF v_input<>v_expected THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The server roll set is incomplete.'; END IF;
        SELECT COUNT(*) INTO v_invalid
        FROM JSON_TABLE(p_rolls,'$[*]' COLUMNS(OriginalOwnerUserId CHAR(36) PATH '$.originalOwnerUserId',RoundNumber INT PATH '$.roundNumber',CaseKey VARCHAR(80) PATH '$.caseKey',MarketHashName VARCHAR(300) PATH '$.marketHashName')) input
        LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=p_battle_id AND BINARY participant.UserId=BINARY input.OriginalOwnerUserId
        LEFT JOIN CaseOpeningBattleCases battleCase ON battleCase.BattleId=p_battle_id AND battleCase.RoundNumber=input.RoundNumber AND BINARY battleCase.CaseKey=BINARY input.CaseKey
        LEFT JOIN CaseOpeningBattles battle ON battle.BattleId=p_battle_id
        LEFT JOIN CaseOpeningPriceSnapshotItems price ON price.PriceSnapshotId=battle.PriceSnapshotId AND BINARY price.MarketHashName=BINARY input.MarketHashName
        WHERE participant.UserId IS NULL OR battleCase.CaseKey IS NULL OR price.MarketHashName IS NULL OR price.Price<0;
        IF v_invalid<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='A staged roll does not match the locked battle snapshot.'; END IF;
        INSERT INTO CaseOpeningBattleRolls(BattleRollId,BattleId,OriginalOwnerUserId,RoundNumber,OpeningId,CaseKey,SourceItemId,ItemName,MarketHashName,ImageUrl,RarityKey,RarityName,RarityColor,Wear,IsStatTrak,IsRareSpecial,SupportsStatTrak,LockedValue,CreatedUtc)
        SELECT UUID(),p_battle_id,input.OriginalOwnerUserId,input.RoundNumber,input.OpeningId,input.CaseKey,input.SourceItemId,input.ItemName,input.MarketHashName,input.ImageUrl,input.RarityKey,input.RarityName,input.RarityColor,input.Wear,input.IsStatTrak,input.IsRareSpecial,input.SupportsStatTrak,price.Price,UTC_TIMESTAMP(6)
        FROM JSON_TABLE(p_rolls,'$[*]' COLUMNS(OriginalOwnerUserId CHAR(36) PATH '$.originalOwnerUserId',RoundNumber INT PATH '$.roundNumber',OpeningId CHAR(36) PATH '$.openingId',CaseKey VARCHAR(80) PATH '$.caseKey',SourceItemId VARCHAR(160) PATH '$.sourceItemId',ItemName VARCHAR(255) PATH '$.itemName',MarketHashName VARCHAR(300) PATH '$.marketHashName',ImageUrl VARCHAR(2048) PATH '$.imageUrl',RarityKey VARCHAR(30) PATH '$.rarityKey',RarityName VARCHAR(80) PATH '$.rarityName',RarityColor CHAR(7) PATH '$.rarityColor',Wear VARCHAR(40) PATH '$.wear',IsStatTrak TINYINT PATH '$.isStatTrak',IsRareSpecial TINYINT PATH '$.isRareSpecial',SupportsStatTrak TINYINT PATH '$.supportsStatTrak')) input
        INNER JOIN CaseOpeningBattles battle ON battle.BattleId=p_battle_id
        INNER JOIN CaseOpeningPriceSnapshotItems price ON price.PriceSnapshotId=battle.PriceSnapshotId AND BINARY price.MarketHashName=BINARY input.MarketHashName;
        COMMIT;
    END IF;
END//

-- The first release supports duels. Other modes remain represented in the schema, but cannot
-- be settled until their approved allocation rules are implemented in a later phase.
DROP PROCEDURE IF EXISTS sp_case_battles_settle_staged//
CREATE PROCEDURE sp_case_battles_settle_staged(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_winner CHAR(36) DEFAULT NULL;
    DECLARE v_rolls INT DEFAULT 0;
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_capacity INT DEFAULT 0;
    DECLARE v_deliver INT DEFAULT 0;
    DECLARE v_overflow INT DEFAULT 0;
    DECLARE v_status VARCHAR(16) DEFAULT '';
    DECLARE v_mode VARCHAR(16) DEFAULT '';
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Status,Mode INTO v_status,v_mode FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE;
    IF v_status<>'opening' OR v_mode<>'duel' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Only an opening duel can be settled.'; END IF;
    SELECT COUNT(*)*(SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id) INTO v_expected FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_rolls FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
    IF v_rolls<>v_expected THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The verified server roll set is incomplete.'; END IF;
    UPDATE CaseOpeningBattleParticipants participant SET TotalValue=(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls roll WHERE roll.BattleId=p_battle_id AND roll.OriginalOwnerUserId=participant.UserId) WHERE participant.BattleId=p_battle_id;
    SELECT participant.UserId INTO v_winner FROM CaseOpeningBattleParticipants participant WHERE participant.BattleId=p_battle_id
    ORDER BY participant.TotalValue DESC,
        (SELECT COALESCE(MAX(LockedValue),0) FROM CaseOpeningBattleRolls roll WHERE roll.BattleId=p_battle_id AND roll.OriginalOwnerUserId=participant.UserId) DESC,
        (SELECT COALESCE(MIN(RoundNumber),9999) FROM CaseOpeningBattleRolls roll WHERE roll.BattleId=p_battle_id AND roll.OriginalOwnerUserId=participant.UserId AND roll.LockedValue=(SELECT MAX(LockedValue) FROM CaseOpeningBattleRolls maxRoll WHERE maxRoll.BattleId=p_battle_id AND maxRoll.OriginalOwnerUserId=participant.UserId)) ASC,
        participant.Seat ASC LIMIT 1;
    UPDATE CaseOpeningBattleRolls SET AwardedToUserId=v_winner WHERE BattleId=p_battle_id;
    SELECT GREATEST(capacity.BaseCapacity+COALESCE((SELECT SUM(AddedSlots) FROM CaseOpeningStorageContainers WHERE UserId=v_winner),0)+COALESCE(upgrades.BonusInventorySlots,0)-(SELECT COUNT(*) FROM CaseOpeningHistory WHERE UserId=v_winner)-(SELECT COALESCE(SUM(Quantity),0) FROM CaseOpeningOwnedCases WHERE UserId=v_winner),0)
    INTO v_capacity FROM CaseOpeningInventoryCapacity capacity LEFT JOIN CaseOpeningUserInventoryUpgrades upgrades ON upgrades.UserId=capacity.UserId WHERE capacity.UserId=v_winner FOR UPDATE;
    SET v_deliver=LEAST(v_rolls,COALESCE(v_capacity,0));
    SET v_overflow=v_rolls-v_deliver;
    IF v_overflow>(SELECT ReservedSlots FROM CaseOpeningBattleOverflowReservations WHERE BattleId=p_battle_id AND UserId=v_winner AND ReleasedUtc IS NULL) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The winner no longer has sufficient reserved Battle Overflow.'; END IF;
    INSERT INTO CaseOpeningBattleSettlements(BattleSettlementId,BattleId,BattleRollId,RecipientUserId,Delivery,LockedValue,AllocationReason,CreatedUtc)
    SELECT UUID(),p_battle_id,ordered.BattleRollId,v_winner,IF(ordered.RowNumber<=v_deliver,'inventory','overflow'),ordered.LockedValue,'duel winner',UTC_TIMESTAMP(6)
    FROM (SELECT BattleRollId,LockedValue,ROW_NUMBER() OVER (ORDER BY RoundNumber,BattleRollId) RowNumber FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id) ordered;
    INSERT INTO CaseOpeningHistory(OpeningId,BattleId,UserId,OriginalOwnerUserId,CaseKey,SourceItemId,ItemName,MarketHashName,ImageUrl,Description,WeaponName,PatternName,PaintIndex,Phase,RarityKey,RarityName,RarityColor,Wear,IsStatTrak,IsRareSpecial,SupportsStatTrak,EstimatedPrice,OpenedUtc)
    SELECT roll.OpeningId,p_battle_id,v_winner,roll.OriginalOwnerUserId,roll.CaseKey,roll.SourceItemId,roll.ItemName,roll.MarketHashName,roll.ImageUrl,'','','','','',roll.RarityKey,roll.RarityName,roll.RarityColor,roll.Wear,roll.IsStatTrak,roll.IsRareSpecial,roll.SupportsStatTrak,roll.LockedValue,UTC_TIMESTAMP(6)
    FROM CaseOpeningBattleRolls roll INNER JOIN CaseOpeningBattleSettlements settlement ON settlement.BattleRollId=roll.BattleRollId WHERE settlement.BattleId=p_battle_id AND settlement.Delivery='inventory';
    INSERT INTO CaseOpeningBattlePulls(BattlePullId,BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,IsSoldForSplit)
    SELECT UUID(),BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,0 FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
    UPDATE CaseOpeningBattleParticipants participant SET AwardedValue=IF(participant.UserId=v_winner,(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id),0) WHERE participant.BattleId=p_battle_id;
    UPDATE CaseOpeningBattleOverflow overflowRow INNER JOIN CaseOpeningBattleOverflowReservations reservation ON reservation.UserId=overflowRow.UserId
    SET overflowRow.OccupiedSlots=overflowRow.OccupiedSlots+IF(reservation.UserId=v_winner,v_overflow,0),overflowRow.UpdatedUtc=UTC_TIMESTAMP(6),reservation.OccupiedSlots=IF(reservation.UserId=v_winner,v_overflow,0),reservation.ReleasedUtc=UTC_TIMESTAMP(6)
    WHERE reservation.BattleId=p_battle_id AND reservation.ReleasedUtc IS NULL;
    UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
    UPDATE CaseOpeningBattles SET Status='settled',WinningUserId=v_winner,WinnerTieBreak='total, highest single pull, earliest locked round, seat',SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
    COMMIT;
END//

DELIMITER ;

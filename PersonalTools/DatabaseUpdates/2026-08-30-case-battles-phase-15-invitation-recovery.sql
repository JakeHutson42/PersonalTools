-- Invitation concurrency, unambiguous escrow refunds, and administrator cancellation.
USE PersonalTools;
DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_reservations_release//
CREATE PROCEDURE sp_case_battles_reservations_release(IN p_battle_id CHAR(36))
BEGIN
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
    SELECT reservation.UserId,reservation.CaseKey,reservation.Quantity,UTC_TIMESTAMP(6)
    FROM CaseOpeningBattleCaseReservations reservation
    WHERE reservation.BattleId=p_battle_id AND reservation.ReleasedUtc IS NULL
    ON DUPLICATE KEY UPDATE
        Quantity=CaseOpeningOwnedCases.Quantity+VALUES(Quantity),
        UpdatedUtc=VALUES(UpdatedUtc);
    UPDATE CaseOpeningBattleCaseReservations SET ReleasedUtc=UTC_TIMESTAMP(6)
    WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
    UPDATE CaseOpeningBattleOverflowReservations SET ReleasedUtc=UTC_TIMESTAMP(6)
    WHERE BattleId=p_battle_id AND ReleasedUtc IS NULL;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invite_set//
CREATE PROCEDURE sp_case_battles_invite_set(IN p_battle_id CHAR(36),IN p_creator_user_id CHAR(36),IN p_invited_user_id CHAR(36))
BEGIN
    DECLARE v_creator_count INT DEFAULT 0;
    DECLARE v_invited_count INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM Users WHERE UserId=p_invited_user_id AND IsActive=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user is no longer available to invite.'; END IF;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_creator_count
    FROM CaseOpeningBattles battle
    LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId
    WHERE battle.Status IN ('waiting','opening') AND (battle.InviteExpiresUtc IS NULL OR battle.InviteExpiresUtc>UTC_TIMESTAMP(6))
      AND (battle.CreatorUserId=p_creator_user_id OR battle.InvitedUserId=p_creator_user_id OR participant.UserId=p_creator_user_id);
    IF v_creator_count>5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You can have up to five unresolved case battles at once.'; END IF;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_invited_count
    FROM CaseOpeningBattles battle
    LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId
    WHERE battle.Status IN ('waiting','opening') AND (battle.InviteExpiresUtc IS NULL OR battle.InviteExpiresUtc>UTC_TIMESTAMP(6))
      AND (battle.CreatorUserId=p_invited_user_id OR battle.InvitedUserId=p_invited_user_id OR participant.UserId=p_invited_user_id);
    IF v_invited_count>=5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That user already has five unresolved case battles.'; END IF;
    UPDATE CaseOpeningBattles SET InvitedUserId=p_invited_user_id,InviteExpiresUtc=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 15 SECOND),ExpiresUtc=DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 15 SECOND)
    WHERE BattleId=p_battle_id AND CreatorUserId=p_creator_user_id AND Status='waiting';
    IF ROW_COUNT()<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The battle could not be sent as an invitation.'; END IF;
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_invitations_get//
CREATE PROCEDURE sp_case_battles_invitations_get(IN p_user_id CHAR(36))
BEGIN
    SELECT battle.BattleId,creator.DisplayName AS CreatorDisplayName,
           (SELECT JSON_ARRAYAGG(battleCase.CaseKey ORDER BY battleCase.RoundNumber) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=battle.BattleId) AS CaseKeys,
           battle.InviteExpiresUtc AS ExpiresUtc
    FROM CaseOpeningBattles battle
    INNER JOIN Users creator ON creator.UserId=battle.CreatorUserId
    WHERE battle.InvitedUserId=p_user_id AND battle.Status='waiting' AND battle.InviteExpiresUtc>UTC_TIMESTAMP(6)
    ORDER BY battle.CreatedUtc ASC
    LIMIT 5;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_pending_created_get//
CREATE PROCEDURE sp_case_battles_pending_created_get(IN p_user_id CHAR(36))
BEGIN
    SELECT battle.BattleId,COALESCE(opponent.DisplayName,'Waiting for opponent') AS OpponentDisplayName,
           (SELECT COUNT(*) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=battle.BattleId) AS CaseCount,
           battle.ExpiresUtc
    FROM CaseOpeningBattles battle
    LEFT JOIN Users opponent ON opponent.UserId=battle.InvitedUserId
    WHERE battle.CreatorUserId=p_user_id AND battle.Status='waiting' AND battle.ExpiresUtc>UTC_TIMESTAMP(6)
    ORDER BY battle.CreatedUtc DESC
    LIMIT 5;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_bot_join//
CREATE PROCEDURE sp_case_battles_bot_join(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_creator CHAR(36) DEFAULT NULL;
    DECLARE v_creator_count INT DEFAULT 0;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattleBotSettings WHERE SettingsId=1 AND Enabled=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Battle Bot is disabled.'; END IF;
    IF NOT EXISTS(SELECT 1 FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='waiting') OR (SELECT COUNT(*) FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id)<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle cannot be joined by Battle Bot.'; END IF;
    SELECT CreatorUserId INTO v_creator FROM CaseOpeningBattles WHERE BattleId=p_battle_id;
    SELECT COUNT(DISTINCT battle.BattleId) INTO v_creator_count FROM CaseOpeningBattles battle LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId WHERE battle.Status IN ('waiting','opening') AND (battle.InviteExpiresUtc IS NULL OR battle.InviteExpiresUtc>UTC_TIMESTAMP(6)) AND (battle.CreatorUserId=v_creator OR battle.InvitedUserId=v_creator OR participant.UserId=v_creator);
    IF v_creator_count>5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You can have up to five unresolved case battles at once.'; END IF;
    INSERT INTO CaseOpeningBattleBotAcquisitions(BattleId,CaseKey,Quantity) SELECT p_battle_id,CaseKey,COUNT(*) FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey;
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
    SELECT '00000000-0000-0000-0000-000000000001',battleCase.CaseKey,COUNT(*),UTC_TIMESTAMP(6) FROM CaseOpeningBattleCases battleCase WHERE battleCase.BattleId=p_battle_id GROUP BY battleCase.CaseKey
    ON DUPLICATE KEY UPDATE Quantity=CaseOpeningOwnedCases.Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
    CALL sp_case_battles_join(p_battle_id,'00000000-0000-0000-0000-000000000001');
    UPDATE CaseOpeningBattleParticipants SET IsReady=1 WHERE BattleId=p_battle_id AND UserId='00000000-0000-0000-0000-000000000001';
    UPDATE CaseOpeningBattleBotStats SET BattlesAttempted=BattlesAttempted+1,UpdatedUtc=UTC_TIMESTAMP(6) WHERE StatsId=1;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_admin_cancel_pending//
CREATE PROCEDURE sp_case_battles_admin_cancel_pending(IN p_battle_id CHAR(36))
BEGIN
    DECLARE v_status VARCHAR(16) DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Status INTO v_status FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE;
    IF v_status IS NULL OR v_status<>'waiting' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Only a pending battle can be cancelled.'; END IF;
    UPDATE CaseOpeningBattles SET Status='cancelled',CancelledUtc=UTC_TIMESTAMP(6),SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
    CALL sp_case_battles_reservations_release(p_battle_id);
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_admin_reconciliation_get//
CREATE PROCEDURE sp_case_battles_admin_reconciliation_get()
BEGIN
    SELECT battle.BattleId,battle.Status,battle.Mode,creator.DisplayName AS CreatorDisplayName,
           COUNT(DISTINCT participant.UserId) AS JoinedPlayers,COUNT(DISTINCT battleCase.RoundNumber) AS CaseCount,
           COUNT(DISTINCT CONCAT(reservation.UserId,':',reservation.CaseKey)) AS ReservedCaseCount,COUNT(DISTINCT roll.BattleRollId) AS StagedRollCount,
           battle.CreatedUtc,battle.ExpiresUtc,battle.StartedUtc,
           CASE WHEN battle.Status='waiting' AND battle.ExpiresUtc<=UTC_TIMESTAMP(6) THEN 'expired-waiting'
                WHEN battle.Status='waiting' THEN 'pending'
                WHEN battle.Status='opening' THEN 'opening-recovery-required' ELSE 'unresolved' END AS Attention
    FROM CaseOpeningBattles battle
    INNER JOIN Users creator ON creator.UserId=battle.CreatorUserId
    LEFT JOIN CaseOpeningBattleParticipants participant ON participant.BattleId=battle.BattleId
    LEFT JOIN CaseOpeningBattleCases battleCase ON battleCase.BattleId=battle.BattleId
    LEFT JOIN CaseOpeningBattleCaseReservations reservation ON reservation.BattleId=battle.BattleId AND reservation.ReleasedUtc IS NULL
    LEFT JOIN CaseOpeningBattleRolls roll ON roll.BattleId=battle.BattleId
    WHERE battle.Status IN ('waiting','opening')
    GROUP BY battle.BattleId,battle.Status,battle.Mode,creator.DisplayName,battle.CreatedUtc,battle.ExpiresUtc,battle.StartedUtc
    ORDER BY battle.CreatedUtc ASC;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_cases_buy_all//
CREATE PROCEDURE sp_case_battles_cases_buy_all(IN p_user_id CHAR(36),IN p_purchases JSON)
BEGIN
    DECLARE v_total_quantity INT DEFAULT 0; DECLARE v_stars_cost BIGINT DEFAULT 0; DECLARE v_gbp_cost BIGINT DEFAULT 0;
    DECLARE v_available_slots INT DEFAULT 0; DECLARE v_purchase_count INT DEFAULT 0; DECLARE v_mode VARCHAR(10) DEFAULT 'stars';
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT COUNT(*),COALESCE(SUM(Quantity),0),COALESCE(SUM(Quantity*CostStars),0),COALESCE(SUM(Quantity*CostGbpPence),0)
    INTO v_purchase_count,v_total_quantity,v_stars_cost,v_gbp_cost
    FROM JSON_TABLE(p_purchases,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$.caseKey',Quantity INT PATH '$.quantity',CostStars INT PATH '$.costStars',CostGbpPence BIGINT PATH '$.costGbpPence')) items;
    IF v_purchase_count=0 OR v_total_quantity<1 OR v_total_quantity>500 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose between 1 and 500 cases to buy.'; END IF;
    IF EXISTS(SELECT 1 FROM JSON_TABLE(p_purchases,'$[*]' COLUMNS(Quantity INT PATH '$.quantity',CostStars INT PATH '$.costStars',CostGbpPence BIGINT PATH '$.costGbpPence')) invalid WHERE Quantity<1 OR CostStars<0 OR CostGbpPence<0) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The case purchase is invalid.'; END IF;
    INSERT IGNORE INTO CaseOpeningInventoryCapacity(UserId,BaseCapacity,UpdatedUtc) VALUES(p_user_id,1000,UTC_TIMESTAMP(6));
    INSERT IGNORE INTO CaseOpeningUserInventoryUpgrades(UserId,UpdatedUtc) VALUES(p_user_id,UTC_TIMESTAMP(6));
    SELECT c.BaseCapacity+COALESCE((SELECT SUM(AddedSlots) FROM CaseOpeningStorageContainers WHERE UserId=p_user_id),0)+u.BonusInventorySlots-(SELECT COUNT(*) FROM CaseOpeningHistory h WHERE h.UserId=p_user_id AND NOT EXISTS(SELECT 1 FROM CaseOpeningTradeUpRecipeHoldings ho WHERE ho.OpeningId=h.OpeningId))-(SELECT COALESCE(SUM(Quantity),0) FROM CaseOpeningOwnedCases WHERE UserId=p_user_id)
    INTO v_available_slots FROM CaseOpeningInventoryCapacity c INNER JOIN CaseOpeningUserInventoryUpgrades u ON u.UserId=c.UserId WHERE c.UserId=p_user_id FOR UPDATE;
    IF v_total_quantity>GREATEST(v_available_slots,0) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough inventory space for every required case.'; END IF;
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,UTC_TIMESTAMP());
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1;
    IF v_mode='gbp' THEN UPDATE CaseOpeningProgress SET GbpPence=GbpPence-v_gbp_cost,UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND GbpPence>=v_gbp_cost; ELSE UPDATE CaseOpeningProgress SET Stars=Stars-v_stars_cost,UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND Stars>=v_stars_cost; END IF;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You do not have enough balance to buy every required case.'; END IF;
    INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc)
    SELECT p_user_id,items.CaseKey,items.PurchaseQuantity,UTC_TIMESTAMP(6) FROM JSON_TABLE(p_purchases,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$.caseKey',PurchaseQuantity INT PATH '$.quantity')) items
    ON DUPLICATE KEY UPDATE Quantity=CaseOpeningOwnedCases.Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
    COMMIT;
    SELECT v_total_quantity PurchasedQuantity,v_stars_cost StarsSpent,v_gbp_cost GbpPenceSpent,Stars StarsBalance,GbpPence GbpPenceBalance FROM CaseOpeningProgress WHERE UserId=p_user_id;
END//
DELIMITER ;

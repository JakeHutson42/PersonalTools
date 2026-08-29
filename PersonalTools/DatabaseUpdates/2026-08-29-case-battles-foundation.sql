-- Case battles are deliberately stored separately from normal openings.  A battle never relies
-- on browser state for case ownership, pricing, or settlement eligibility.
USE PersonalTools;

CREATE TABLE IF NOT EXISTS CaseOpeningBattles
(
    BattleId CHAR(36) NOT NULL,
    CreatorUserId CHAR(36) NOT NULL,
    Mode VARCHAR(16) NOT NULL,
    Status VARCHAR(16) NOT NULL,
    PriceSnapshotId CHAR(36) NULL,
    CreatedUtc DATETIME(6) NOT NULL,
    ExpiresUtc DATETIME(6) NOT NULL,
    StartedUtc DATETIME(6) NULL,
    SettledUtc DATETIME(6) NULL,
    WinningUserId CHAR(36) NULL,
    WinningTeam TINYINT UNSIGNED NULL,
    PRIMARY KEY (BattleId),
    KEY IX_CaseOpeningBattles_Status_Expires (Status, ExpiresUtc),
    KEY IX_CaseOpeningBattles_Creator (CreatorUserId, CreatedUtc),
    CONSTRAINT FK_CaseOpeningBattles_Creator FOREIGN KEY (CreatorUserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS CaseOpeningBattleCases
(
    BattleId CHAR(36) NOT NULL,
    RoundNumber SMALLINT UNSIGNED NOT NULL,
    CaseKey VARCHAR(80) NOT NULL,
    PRIMARY KEY (BattleId, RoundNumber),
    CONSTRAINT FK_CaseOpeningBattleCases_Battle FOREIGN KEY (BattleId) REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS CaseOpeningBattleParticipants
(
    BattleId CHAR(36) NOT NULL,
    UserId CHAR(36) NOT NULL,
    Seat TINYINT UNSIGNED NOT NULL,
    Team TINYINT UNSIGNED NOT NULL DEFAULT 0,
    IsReady TINYINT(1) NOT NULL DEFAULT 0,
    OverflowReservedSlots SMALLINT UNSIGNED NOT NULL DEFAULT 0,
    TotalValue DECIMAL(12,2) NOT NULL DEFAULT 0,
    AwardedValue DECIMAL(12,2) NOT NULL DEFAULT 0,
    JoinedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY (BattleId, UserId),
    UNIQUE KEY UX_CaseOpeningBattleParticipants_Seat (BattleId, Seat),
    KEY IX_CaseOpeningBattleParticipants_User (UserId, JoinedUtc),
    CONSTRAINT FK_CaseOpeningBattleParticipants_Battle FOREIGN KEY (BattleId) REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE,
    CONSTRAINT FK_CaseOpeningBattleParticipants_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS CaseOpeningBattlePulls
(
    BattlePullId CHAR(36) NOT NULL,
    BattleId CHAR(36) NOT NULL,
    OpeningId CHAR(36) NOT NULL,
    OriginalOwnerUserId CHAR(36) NOT NULL,
    RoundNumber SMALLINT UNSIGNED NOT NULL,
    LockedValue DECIMAL(12,2) NOT NULL,
    AwardedToUserId CHAR(36) NULL,
    IsSoldForSplit TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (BattlePullId),
    UNIQUE KEY UX_CaseOpeningBattlePulls_Opening (OpeningId),
    KEY IX_CaseOpeningBattlePulls_Battle (BattleId, RoundNumber),
    CONSTRAINT FK_CaseOpeningBattlePulls_Battle FOREIGN KEY (BattleId) REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE
);

-- Rolls are staged by the server before settlement.  The final settlement procedure will read
-- only these rows, never browser-provided winners, prices, or item payloads.
CREATE TABLE IF NOT EXISTS CaseOpeningBattleRolls
(
    BattleRollId CHAR(36) NOT NULL,
    BattleId CHAR(36) NOT NULL,
    OriginalOwnerUserId CHAR(36) NOT NULL,
    RoundNumber SMALLINT UNSIGNED NOT NULL,
    OpeningId CHAR(36) NOT NULL,
    CaseKey VARCHAR(80) NOT NULL,
    SourceItemId VARCHAR(160) NOT NULL,
    ItemName VARCHAR(255) NOT NULL,
    MarketHashName VARCHAR(300) NOT NULL,
    ImageUrl VARCHAR(2048) NOT NULL,
    RarityKey VARCHAR(30) NOT NULL,
    RarityName VARCHAR(80) NOT NULL,
    RarityColor CHAR(7) NOT NULL,
    Wear VARCHAR(40) NOT NULL,
    IsStatTrak TINYINT(1) NOT NULL DEFAULT 0,
    IsRareSpecial TINYINT(1) NOT NULL DEFAULT 0,
    SupportsStatTrak TINYINT(1) NOT NULL DEFAULT 0,
    LockedValue DECIMAL(12,2) NOT NULL,
    AwardedToUserId CHAR(36) NULL,
    IsSoldForSplit TINYINT(1) NOT NULL DEFAULT 0,
    CreatedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY (BattleRollId),
    UNIQUE KEY UX_CaseOpeningBattleRolls_BattleOwnerRound (BattleId,OriginalOwnerUserId,RoundNumber),
    KEY IX_CaseOpeningBattleRolls_Battle (BattleId,RoundNumber),
    CONSTRAINT FK_CaseOpeningBattleRolls_Battle FOREIGN KEY (BattleId) REFERENCES CaseOpeningBattles(BattleId) ON DELETE CASCADE
);

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_battles_create//
CREATE PROCEDURE sp_case_battles_create(IN p_battle_id CHAR(36), IN p_user_id CHAR(36), IN p_mode VARCHAR(16), IN p_case_keys JSON)
BEGIN
    DECLARE v_case_count INT DEFAULT 0;
    DECLARE v_missing_cases INT DEFAULT 0;
    DECLARE v_required_players INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SET v_required_players=CASE p_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 WHEN 'ffa-4' THEN 4 WHEN 'teams-2v2' THEN 4 ELSE 0 END;
    SET v_case_count=JSON_LENGTH(p_case_keys);
    IF v_required_players=0 OR v_case_count<1 OR v_case_count>20 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose a valid battle mode and between 1 and 20 cases.'; END IF;
    SELECT COUNT(*) INTO v_missing_cases
    FROM (SELECT CaseKey,COUNT(*) RequiredQuantity FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$')) c GROUP BY CaseKey) required
    LEFT JOIN CaseOpeningOwnedCases o ON o.UserId=p_user_id AND o.CaseKey=required.CaseKey
    WHERE COALESCE(o.Quantity,0)<required.RequiredQuantity;
    IF v_missing_cases<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You need to own every selected case before creating a battle.'; END IF;
    INSERT INTO CaseOpeningBattles(BattleId,CreatorUserId,Mode,Status,PriceSnapshotId,CreatedUtc,ExpiresUtc)
    SELECT p_battle_id,p_user_id,p_mode,'waiting',PriceSnapshotId,UTC_TIMESTAMP(6),DATE_ADD(UTC_TIMESTAMP(6),INTERVAL 10 MINUTE)
    FROM CaseOpeningPriceSnapshots WHERE IsActive=1 LIMIT 1;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='An active price snapshot is required before creating a battle.'; END IF;
    INSERT INTO CaseOpeningBattleCases(BattleId,RoundNumber,CaseKey)
    SELECT p_battle_id,ord-1,CaseKey FROM JSON_TABLE(p_case_keys,'$[*]' COLUMNS(ord FOR ORDINALITY,CaseKey VARCHAR(80) PATH '$')) c;
    INSERT INTO CaseOpeningBattleParticipants(BattleId,UserId,Seat,Team,OverflowReservedSlots,JoinedUtc)
    VALUES(p_battle_id,p_user_id,1,IF(p_mode='teams-2v2',1,0),v_case_count,UTC_TIMESTAMP(6));
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_get//
CREATE PROCEDURE sp_case_battles_get(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT b.BattleId,b.CreatorUserId,b.Mode,b.Status,b.CreatedUtc,b.ExpiresUtc,b.WinningUserId,b.WinningTeam,
       (SELECT COUNT(*) FROM CaseOpeningBattleParticipants p WHERE p.BattleId=b.BattleId) JoinedPlayers,
       (SELECT JSON_ARRAYAGG(c.CaseKey ORDER BY c.RoundNumber) FROM CaseOpeningBattleCases c WHERE c.BattleId=b.BattleId) CaseKeys,
       EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants me WHERE me.BattleId=b.BattleId AND me.UserId=p_user_id) IsParticipant
    FROM CaseOpeningBattles b WHERE b.BattleId=p_battle_id;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_active_get//
CREATE PROCEDURE sp_case_battles_active_get(IN p_user_id CHAR(36))
BEGIN
    SELECT b.BattleId,b.CreatorUserId,b.Mode,b.Status,b.CreatedUtc,b.ExpiresUtc,
       (SELECT COUNT(*) FROM CaseOpeningBattleParticipants p2 WHERE p2.BattleId=b.BattleId) JoinedPlayers,
       (SELECT JSON_ARRAYAGG(c.CaseKey ORDER BY c.RoundNumber) FROM CaseOpeningBattleCases c WHERE c.BattleId=b.BattleId) CaseKeys
    FROM CaseOpeningBattleParticipants p INNER JOIN CaseOpeningBattles b ON b.BattleId=p.BattleId
    WHERE p.UserId=p_user_id AND b.Status IN ('waiting','ready','opening') ORDER BY b.CreatedUtc DESC LIMIT 1;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_join//
CREATE PROCEDURE sp_case_battles_join(IN p_battle_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    DECLARE v_mode VARCHAR(16); DECLARE v_status VARCHAR(16); DECLARE v_required INT; DECLARE v_joined INT; DECLARE v_cases INT; DECLARE v_missing INT;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Mode,Status INTO v_mode,v_status FROM CaseOpeningBattles WHERE BattleId=p_battle_id FOR UPDATE;
    IF v_status IS NULL OR v_status<>'waiting' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle is no longer accepting players.'; END IF;
    IF EXISTS(SELECT 1 FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND UserId=p_user_id) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You have already joined this battle.'; END IF;
    SET v_required=CASE v_mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 ELSE 4 END;
    SELECT COUNT(*) INTO v_joined FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id;
    IF v_joined>=v_required THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle is full.'; END IF;
    SELECT COUNT(*) INTO v_cases FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id;
    SELECT COUNT(*) INTO v_missing FROM (SELECT CaseKey,COUNT(*) RequiredQuantity FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id GROUP BY CaseKey) required
    LEFT JOIN CaseOpeningOwnedCases o ON o.UserId=p_user_id AND o.CaseKey=required.CaseKey WHERE COALESCE(o.Quantity,0)<required.RequiredQuantity;
    IF v_missing<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You need to own every selected case before joining.'; END IF;
    INSERT INTO CaseOpeningBattleParticipants(BattleId,UserId,Seat,Team,OverflowReservedSlots,JoinedUtc)
    VALUES(p_battle_id,p_user_id,v_joined+1,IF(v_mode='teams-2v2',IF(v_joined<2,1,2),0),v_cases,UTC_TIMESTAMP(6));
    COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_history_get//
CREATE PROCEDURE sp_case_battles_history_get(IN p_user_id CHAR(36))
BEGIN
    SELECT b.BattleId,b.Mode,b.Status,p.TotalValue,p.AwardedValue,b.SettledUtc,
       IF((b.Mode='teams-2v2' AND p.Team=b.WinningTeam) OR (b.Mode<>'teams-2v2' AND b.WinningUserId=p_user_id),1,0) Won
    FROM CaseOpeningBattleParticipants p INNER JOIN CaseOpeningBattles b ON b.BattleId=p.BattleId
    WHERE p.UserId=p_user_id AND b.Status IN ('settled','cancelled') ORDER BY COALESCE(b.SettledUtc,b.CreatedUtc) DESC LIMIT 50;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_ready_set//
CREATE PROCEDURE sp_case_battles_ready_set(IN p_battle_id CHAR(36), IN p_user_id CHAR(36), IN p_is_ready TINYINT)
BEGIN
    UPDATE CaseOpeningBattleParticipants p INNER JOIN CaseOpeningBattles b ON b.BattleId=p.BattleId
    SET p.IsReady=IF(p_is_ready<>0,1,0)
    WHERE p.BattleId=p_battle_id AND p.UserId=p_user_id AND b.Status='waiting' AND b.ExpiresUtc>UTC_TIMESTAMP(6);
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle can no longer be readied.'; END IF;
END//

DROP PROCEDURE IF EXISTS sp_case_battles_settle_staged//
CREATE PROCEDURE sp_case_battles_settle_staged(IN p_battle_id CHAR(36))
BEGIN
 DECLARE v_required INT DEFAULT 0; DECLARE v_ready INT DEFAULT 0; DECLARE v_expected INT DEFAULT 0; DECLARE v_rolls INT DEFAULT 0; DECLARE v_invalid INT DEFAULT 0;
 DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
 START TRANSACTION;
 SELECT CASE Mode WHEN 'duel' THEN 2 WHEN 'ffa-3' THEN 3 ELSE 4 END INTO v_required FROM CaseOpeningBattles WHERE BattleId=p_battle_id AND Status='opening' FOR UPDATE;
 IF v_required=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This battle cannot be settled.'; END IF;
 SELECT COUNT(*) INTO v_ready FROM CaseOpeningBattleParticipants WHERE BattleId=p_battle_id AND IsReady=1;
 SELECT COUNT(*)*v_required INTO v_expected FROM CaseOpeningBattleCases WHERE BattleId=p_battle_id;
 SELECT COUNT(*) INTO v_rolls FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id AND AwardedToUserId IS NOT NULL;
 SELECT COUNT(*) INTO v_invalid FROM CaseOpeningBattleRolls r LEFT JOIN CaseOpeningBattleParticipants ownerSeat ON ownerSeat.BattleId=r.BattleId AND ownerSeat.UserId=r.OriginalOwnerUserId LEFT JOIN CaseOpeningBattleParticipants awardSeat ON awardSeat.BattleId=r.BattleId AND awardSeat.UserId=r.AwardedToUserId WHERE r.BattleId=p_battle_id AND (ownerSeat.UserId IS NULL OR awardSeat.UserId IS NULL OR r.LockedValue<0);
 IF v_ready<>v_required OR v_rolls<>v_expected OR v_invalid<>0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The verified server roll set is incomplete.'; END IF;
 UPDATE CaseOpeningOwnedCases o INNER JOIN (SELECT p.UserId,c.CaseKey,COUNT(*) Quantity FROM CaseOpeningBattleParticipants p CROSS JOIN CaseOpeningBattleCases c WHERE p.BattleId=p_battle_id GROUP BY p.UserId,c.CaseKey) required ON required.UserId=o.UserId AND required.CaseKey=o.CaseKey SET o.Quantity=o.Quantity-required.Quantity,o.UpdatedUtc=UTC_TIMESTAMP(6) WHERE o.Quantity>=required.Quantity;
 IF ROW_COUNT()<>(SELECT COUNT(*) FROM (SELECT p.UserId,c.CaseKey FROM CaseOpeningBattleParticipants p CROSS JOIN CaseOpeningBattleCases c WHERE p.BattleId=p_battle_id GROUP BY p.UserId,c.CaseKey) x) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='A participant no longer owns every required case.'; END IF;
 INSERT INTO CaseOpeningHistory(OpeningId,UserId,CaseKey,SourceItemId,ItemName,MarketHashName,ImageUrl,Description,WeaponName,PatternName,PaintIndex,Phase,RarityKey,RarityName,RarityColor,Wear,IsStatTrak,IsRareSpecial,SupportsStatTrak,EstimatedPrice,OpenedUtc) SELECT OpeningId,AwardedToUserId,CaseKey,SourceItemId,ItemName,MarketHashName,ImageUrl,'','','','','',RarityKey,RarityName,RarityColor,Wear,IsStatTrak,IsRareSpecial,SupportsStatTrak,LockedValue,UTC_TIMESTAMP(6) FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
 INSERT INTO CaseOpeningBattlePulls(BattlePullId,BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,IsSoldForSplit) SELECT UUID(),BattleId,OpeningId,OriginalOwnerUserId,RoundNumber,LockedValue,AwardedToUserId,IsSoldForSplit FROM CaseOpeningBattleRolls WHERE BattleId=p_battle_id;
 UPDATE CaseOpeningBattleParticipants p SET TotalValue=(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls r WHERE r.BattleId=p_battle_id AND r.OriginalOwnerUserId=p.UserId),AwardedValue=(SELECT COALESCE(SUM(LockedValue),0) FROM CaseOpeningBattleRolls r WHERE r.BattleId=p_battle_id AND r.AwardedToUserId=p.UserId) WHERE p.BattleId=p_battle_id;
 UPDATE CaseOpeningBattles SET Status='settled',SettledUtc=UTC_TIMESTAMP(6) WHERE BattleId=p_battle_id;
 COMMIT;
END//

-- Buy-all is intentionally one transaction.  Calling the regular single-case purchase procedure
-- in a loop could leave a player with a partially funded battle requirement after a later case fails.
DROP PROCEDURE IF EXISTS sp_case_battles_cases_buy_all//
CREATE PROCEDURE sp_case_battles_cases_buy_all(IN p_user_id CHAR(36), IN p_purchases JSON)
BEGIN
    DECLARE v_total_quantity INT DEFAULT 0; DECLARE v_stars_cost BIGINT DEFAULT 0; DECLARE v_gbp_cost BIGINT DEFAULT 0;
    DECLARE v_available_slots INT DEFAULT 0; DECLARE v_purchase_count INT DEFAULT 0;
    DECLARE v_mode VARCHAR(10) DEFAULT 'stars';
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT COUNT(*),COALESCE(SUM(Quantity),0),COALESCE(SUM(Quantity*CostStars),0),COALESCE(SUM(Quantity*CostGbpPence),0)
    INTO v_purchase_count,v_total_quantity,v_stars_cost,v_gbp_cost
    FROM JSON_TABLE(p_purchases,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$.caseKey', Quantity INT PATH '$.quantity', CostStars INT PATH '$.costStars', CostGbpPence BIGINT PATH '$.costGbpPence')) items;
    IF v_purchase_count=0 OR v_total_quantity<1 OR v_total_quantity>500 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose between 1 and 500 cases to buy.'; END IF;
    IF EXISTS(SELECT 1 FROM JSON_TABLE(p_purchases,'$[*]' COLUMNS(Quantity INT PATH '$.quantity', CostStars INT PATH '$.costStars', CostGbpPence BIGINT PATH '$.costGbpPence')) invalid WHERE Quantity<1 OR CostStars<0 OR CostGbpPence<0) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='The case purchase is invalid.'; END IF;
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
    SELECT p_user_id,CaseKey,PurchaseQuantity,UTC_TIMESTAMP(6) FROM JSON_TABLE(p_purchases,'$[*]' COLUMNS(CaseKey VARCHAR(80) PATH '$.caseKey', PurchaseQuantity INT PATH '$.quantity')) items
    ON DUPLICATE KEY UPDATE Quantity=Quantity+VALUES(Quantity),UpdatedUtc=VALUES(UpdatedUtc);
    COMMIT;
    SELECT v_total_quantity PurchasedQuantity,v_stars_cost StarsSpent,v_gbp_cost GbpPenceSpent,Stars StarsBalance,GbpPence GbpPenceBalance FROM CaseOpeningProgress WHERE UserId=p_user_id;
END//
DELIMITER ;

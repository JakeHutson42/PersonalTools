USE PersonalTools;

-- Buy All is for a case battle's required cases. It deliberately does not require
-- the normal case-opening unlock state; currency, capacity and atomicity remain enforced.
DELIMITER //
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

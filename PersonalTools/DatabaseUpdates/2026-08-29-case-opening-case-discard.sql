DROP PROCEDURE IF EXISTS sp_case_opening_cases_discard;
DELIMITER //
CREATE PROCEDURE sp_case_opening_cases_discard(
    IN p_user_id CHAR(36),
    IN p_case_key VARCHAR(128),
    IN p_quantity INT
)
BEGIN
    DECLARE v_owned_quantity INT DEFAULT 0;
    DECLARE v_discarded_quantity INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;

    START TRANSACTION;
    SELECT COALESCE(Quantity, 0) INTO v_owned_quantity
    FROM CaseOpeningOwnedCases
    WHERE UserId = p_user_id AND CaseKey = p_case_key
    FOR UPDATE;

    IF v_owned_quantity < 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'You do not own any of these cases.';
    END IF;

    SET v_discarded_quantity = IF(p_quantity = 0, v_owned_quantity, LEAST(p_quantity, v_owned_quantity));
    UPDATE CaseOpeningOwnedCases
    SET Quantity = Quantity - v_discarded_quantity, UpdatedUtc = UTC_TIMESTAMP(6)
    WHERE UserId = p_user_id AND CaseKey = p_case_key;
    DELETE FROM CaseOpeningOwnedCases WHERE UserId = p_user_id AND CaseKey = p_case_key AND Quantity <= 0;
    COMMIT;

    SELECT p_case_key AS CaseKey, v_discarded_quantity AS DiscardedQuantity,
        COALESCE((SELECT Quantity FROM CaseOpeningOwnedCases WHERE UserId = p_user_id AND CaseKey = p_case_key), 0) AS OwnedQuantity;
END//
DELIMITER ;

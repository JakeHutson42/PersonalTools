-- Phase 5A: opening quality-of-life nodes and the one cross-device opening preference.
ALTER TABLE CaseOpeningUpgradeDefinitions ADD COLUMN IF NOT EXISTS CostGbpPence BIGINT NOT NULL DEFAULT 0 AFTER CostStars;

INSERT INTO CaseOpeningUpgradeDefinitions
    (UpgradeKey,Name,Description,Category,CostStars,CostGbpPence,RequiredLevel,SortOrder,IsActive)
VALUES
    ('instant-repeat','Instant Repeat','Show a one-tap repeat action after a completed opening.','opening-qol',1800,180,8,610,1),
    ('remember-opening-quantity','Remember Opening Quantity','Remember the selected opening quantity across devices.','opening-qol',2400,240,10,620,1),
    ('streamlined-results','Streamlined Results','Use a compact result layout that keeps the next action close.','opening-qol',3200,320,12,630,1),
    ('batch-reveal','Batch Reveal','Reveal every result in a multi-open together, including rare-special items.','opening-qol',4500,450,15,640,1)
ON DUPLICATE KEY UPDATE
    Name=VALUES(Name),Description=VALUES(Description),Category=VALUES(Category),SortOrder=VALUES(SortOrder),IsActive=VALUES(IsActive);

CREATE TABLE IF NOT EXISTS CaseOpeningUserPreferences (
    UserId CHAR(36) NOT NULL,
    LastOpenQuantity INT NOT NULL DEFAULT 1,
    UpdatedUtc DATETIME(6) NOT NULL DEFAULT UTC_TIMESTAMP(6),
    PRIMARY KEY (UserId),
    CONSTRAINT FK_CaseOpeningUserPreferences_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_user_preferences_get//
CREATE PROCEDURE sp_case_opening_user_preferences_get(IN p_user_id CHAR(36))
BEGIN
    INSERT IGNORE INTO CaseOpeningUserPreferences(UserId) VALUES(p_user_id);
    SELECT LastOpenQuantity FROM CaseOpeningUserPreferences WHERE UserId=p_user_id;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_last_quantity_set//
CREATE PROCEDURE sp_case_opening_last_quantity_set(IN p_user_id CHAR(36), IN p_quantity INT)
BEGIN
    IF p_quantity<1 OR p_quantity>5 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose an opening quantity from 1 to 5.'; END IF;
    INSERT INTO CaseOpeningUserPreferences(UserId,LastOpenQuantity,UpdatedUtc)
    VALUES(p_user_id,p_quantity,UTC_TIMESTAMP(6))
    ON DUPLICATE KEY UPDATE LastOpenQuantity=VALUES(LastOpenQuantity),UpdatedUtc=UTC_TIMESTAMP(6);
    SELECT LastOpenQuantity FROM CaseOpeningUserPreferences WHERE UserId=p_user_id;
END//
DELIMITER ;

-- Account-specific, administrator-controlled drop overrides. Empty rows mean normal live odds.
CREATE TABLE IF NOT EXISTS CaseOpeningDevDropRarities(
    UserId CHAR(36) NOT NULL,RarityGroup VARCHAR(20) NOT NULL,UpdatedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY(UserId,RarityGroup),CONSTRAINT FK_CaseOpeningDevDropRarities_Users FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_opening_dev_drop_rarities_get//
CREATE PROCEDURE sp_case_opening_dev_drop_rarities_get(IN p_user_id CHAR(36))
BEGIN
    SELECT RarityGroup FROM CaseOpeningDevDropRarities WHERE UserId=p_user_id ORDER BY FIELD(RarityGroup,'blue','purple','pink','red','gold');
END//

DROP PROCEDURE IF EXISTS sp_case_opening_dev_drop_rarities_set//
CREATE PROCEDURE sp_case_opening_dev_drop_rarities_set(IN p_user_id CHAR(36),IN p_rarity_groups JSON)
BEGIN
    DELETE FROM CaseOpeningDevDropRarities WHERE UserId=p_user_id;
    INSERT INTO CaseOpeningDevDropRarities(UserId,RarityGroup,UpdatedUtc)
    SELECT p_user_id,x.RarityGroup,UTC_TIMESTAMP(6)
    FROM JSON_TABLE(p_rarity_groups,'$[*]' COLUMNS(RarityGroup VARCHAR(20) PATH '$')) x
    WHERE x.RarityGroup IN ('blue','purple','pink','red','gold');
END//

DELIMITER ;

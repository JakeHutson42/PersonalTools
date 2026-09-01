CREATE TABLE IF NOT EXISTS CaseOpeningSkillTreeSettings (
    SettingsId TINYINT NOT NULL,
    Enabled TINYINT(1) NOT NULL DEFAULT 0,
    UpdatedUtc DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY (SettingsId)
) ENGINE=InnoDB;

INSERT INTO CaseOpeningSkillTreeSettings (SettingsId, Enabled)
VALUES (1, 0)
ON DUPLICATE KEY UPDATE SettingsId = VALUES(SettingsId);

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_opening_skill_tree_settings_get//
CREATE PROCEDURE sp_case_opening_skill_tree_settings_get()
BEGIN
    SELECT Enabled
    FROM CaseOpeningSkillTreeSettings
    WHERE SettingsId = 1;
END//

DROP PROCEDURE IF EXISTS sp_case_opening_skill_tree_settings_set//
CREATE PROCEDURE sp_case_opening_skill_tree_settings_set(IN p_enabled TINYINT)
BEGIN
    UPDATE CaseOpeningSkillTreeSettings
    SET Enabled = IF(p_enabled = 1, 1, 0),
        UpdatedUtc = UTC_TIMESTAMP()
    WHERE SettingsId = 1;
END//

DELIMITER ;

-- Admin-managed Case Battles visibility. The app configuration flag remains an emergency deployment brake.
USE PersonalTools;
ALTER TABLE CaseOpeningBattleBotSettings
    ADD COLUMN IF NOT EXISTS CaseBattlesEnabled TINYINT(1) NOT NULL DEFAULT 0 AFTER SettingsId;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battles_bot_status_get//
CREATE PROCEDURE sp_case_battles_bot_status_get()
BEGIN
    SELECT settings.CaseBattlesEnabled,settings.Enabled,stats.BattlesAttempted,stats.BattlesWon,stats.SkinsDiscarded,stats.ValueDiscarded
    FROM CaseOpeningBattleBotSettings settings
    INNER JOIN CaseOpeningBattleBotStats stats ON stats.StatsId=1
    WHERE settings.SettingsId=1;
END//
DROP PROCEDURE IF EXISTS sp_case_battles_feature_enabled_set//
CREATE PROCEDURE sp_case_battles_feature_enabled_set(IN p_enabled TINYINT)
BEGIN
    UPDATE CaseOpeningBattleBotSettings SET CaseBattlesEnabled=IF(p_enabled<>0,1,0),UpdatedUtc=UTC_TIMESTAMP(6) WHERE SettingsId=1;
END//
DELIMITER ;

USE PersonalTools;

CREATE TABLE IF NOT EXISTS LiveWinnersSettings (SettingsId TINYINT NOT NULL PRIMARY KEY DEFAULT 1, Visibility VARCHAR(12) NOT NULL DEFAULT 'users', CONSTRAINT CK_LiveWinnersSettings_Visibility CHECK (Visibility IN ('users','admins'))) ENGINE=InnoDB;
INSERT IGNORE INTO LiveWinnersSettings(SettingsId,Visibility) VALUES(1,'users');
CREATE TABLE IF NOT EXISTS UserLivePresence (UserId CHAR(36) NOT NULL PRIMARY KEY, LastSeenUtc DATETIME(6) NOT NULL, CONSTRAINT FK_UserLivePresence_User FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE, KEY IX_UserLivePresence_LastSeen(LastSeenUtc)) ENGINE=InnoDB;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_live_winners_presence_touch//
CREATE PROCEDURE sp_live_winners_presence_touch(IN p_user_id CHAR(36)) BEGIN INSERT INTO UserLivePresence(UserId,LastSeenUtc) VALUES(p_user_id,UTC_TIMESTAMP(6)) ON DUPLICATE KEY UPDATE LastSeenUtc=VALUES(LastSeenUtc); END//
DROP PROCEDURE IF EXISTS sp_live_winners_summary_get//
CREATE PROCEDURE sp_live_winners_summary_get() BEGIN SELECT (SELECT COUNT(*) FROM UserLivePresence WHERE LastSeenUtc>=DATE_SUB(UTC_TIMESTAMP(6),INTERVAL 90 SECOND)) LiveUserCount,(SELECT Visibility FROM LiveWinnersSettings WHERE SettingsId=1) Visibility; END//
DROP PROCEDURE IF EXISTS sp_live_winners_visibility_set//
CREATE PROCEDURE sp_live_winners_visibility_set(IN p_visibility VARCHAR(12)) BEGIN UPDATE LiveWinnersSettings SET Visibility=p_visibility WHERE SettingsId=1; END//
DROP PROCEDURE IF EXISTS sp_live_winners_top_get//
CREATE PROCEDURE sp_live_winners_top_get()
BEGIN
 SELECT u.DisplayName,h.ItemName,h.ImageUrl,h.RarityColor,COALESCE(v.Price,h.EstimatedPrice,0) EstimatedPrice,h.OpenedUtc,IF(h.CaseKey='trade-up','Trade Up','Case opening') Source
 FROM CaseOpeningHistory h INNER JOIN Users u ON u.UserId=h.UserId LEFT JOIN CaseOpeningOpeningSpecialVariants v ON v.OpeningId=h.OpeningId
 WHERE h.OpenedUtc>=DATE_SUB(UTC_TIMESTAMP(6),INTERVAL 24 HOUR) AND COALESCE(v.Price,h.EstimatedPrice) IS NOT NULL
 ORDER BY COALESCE(v.Price,h.EstimatedPrice) DESC,h.OpenedUtc DESC LIMIT 3;
END//
DELIMITER ;

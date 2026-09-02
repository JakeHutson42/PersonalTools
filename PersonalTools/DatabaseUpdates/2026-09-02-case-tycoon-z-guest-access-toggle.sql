-- Runtime kill switch for browser-based Case Tycoon accounts. Disabling access preserves all guest data.
USE PersonalTools;

CREATE TABLE IF NOT EXISTS CaseTycoonFeatureSettings
(
    Id TINYINT UNSIGNED NOT NULL,
    GuestAccessEnabled TINYINT(1) NOT NULL DEFAULT 1,
    UpdatedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY (Id)
) COLLATE='utf8mb4_unicode_ci' ENGINE=InnoDB;

INSERT IGNORE INTO CaseTycoonFeatureSettings(Id,GuestAccessEnabled,UpdatedUtc) VALUES(1,1,UTC_TIMESTAMP(6));

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_tycoon_guest_access_get//
CREATE PROCEDURE sp_case_tycoon_guest_access_get()
    SELECT GuestAccessEnabled AS Enabled FROM CaseTycoonFeatureSettings WHERE Id=1//

DROP PROCEDURE IF EXISTS sp_case_tycoon_guest_access_set//
CREATE PROCEDURE sp_case_tycoon_guest_access_set(IN p_enabled TINYINT)
    UPDATE CaseTycoonFeatureSettings SET GuestAccessEnabled=IF(p_enabled=1,1,0),UpdatedUtc=UTC_TIMESTAMP(6) WHERE Id=1//

DROP PROCEDURE IF EXISTS sp_auth_guest_create//
CREATE PROCEDURE sp_auth_guest_create(IN p_user_id CHAR(36),IN p_email VARCHAR(254),IN p_display_name VARCHAR(100),IN p_password_hash VARCHAR(512),IN p_username VARCHAR(32))
BEGIN
 DECLARE v_guest_access_enabled TINYINT DEFAULT 0;
 DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
 START TRANSACTION;
 SELECT GuestAccessEnabled INTO v_guest_access_enabled FROM CaseTycoonFeatureSettings WHERE Id=1 FOR UPDATE;
 IF v_guest_access_enabled<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Guest access is currently unavailable.'; END IF;
 INSERT INTO Users(UserId,Email,DisplayName,PasswordHash,IsActive,IsGuest,UserRole,CreatedUtc,Username)
 VALUES(p_user_id,p_email,p_display_name,p_password_hash,1,1,1,UTC_TIMESTAMP(),p_username);
 INSERT INTO CaseOpeningUnlockedCases(UserId,CaseKey,UnlockedUtc) VALUES(p_user_id,'kilowatt',UTC_TIMESTAMP());
 INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) VALUES(p_user_id,'kilowatt',25,UTC_TIMESTAMP(6));
 INSERT INTO CaseOpeningInventoryCapacity(UserId,BaseCapacity,UpdatedUtc) VALUES(p_user_id,1000,UTC_TIMESTAMP(6));
 INSERT INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
 COMMIT;
END//
DELIMITER ;

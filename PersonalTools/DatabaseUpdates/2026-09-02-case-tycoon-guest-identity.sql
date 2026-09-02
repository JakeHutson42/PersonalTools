-- Guest accounts are real server-side identities, but their authorization scope is Case Tycoon only.
USE PersonalTools;

ALTER TABLE Users ADD COLUMN IF NOT EXISTS IsGuest TINYINT(1) NOT NULL DEFAULT 0 AFTER IsActive;
CREATE INDEX IF NOT EXISTS IX_Users_IsGuest_Created ON Users(IsGuest,CreatedUtc);

DELIMITER //
DROP PROCEDURE IF EXISTS sp_auth_user_get_by_email//
CREATE PROCEDURE sp_auth_user_get_by_email(IN p_email VARCHAR(254))
 SELECT UserId,Email,DisplayName,PasswordHash,IsActive,IsGuest,SteamId,UserRole AS Role,FailedLoginAttempts,LockoutUntilUtc,LastFailedLoginUtc FROM Users WHERE Email=p_email AND IsGuest=0 LIMIT 1//

DROP PROCEDURE IF EXISTS sp_auth_user_get_by_id//
CREATE PROCEDURE sp_auth_user_get_by_id(IN p_user_id CHAR(36))
 SELECT UserId,Email,DisplayName,PasswordHash,IsActive,IsGuest,SteamId,UserRole AS Role,FailedLoginAttempts,LockoutUntilUtc,LastFailedLoginUtc FROM Users WHERE UserId=p_user_id LIMIT 1//

DROP PROCEDURE IF EXISTS sp_auth_users_get_all//
CREATE PROCEDURE sp_auth_users_get_all()
 SELECT u.UserId,u.Email,u.DisplayName,u.IsActive,u.UserRole AS Role,u.CreatedUtc,u.FailedLoginAttempts,u.LockoutUntilUtc,u.LastFailedLoginUtc,MAX(s.CreatedUtc) LastLoginUtc
 FROM Users u LEFT JOIN UserSessions s ON s.UserId=u.UserId WHERE u.IsGuest=0
 GROUP BY u.UserId,u.Email,u.DisplayName,u.IsActive,u.UserRole,u.CreatedUtc,u.FailedLoginAttempts,u.LockoutUntilUtc,u.LastFailedLoginUtc ORDER BY u.DisplayName,u.Email//

DROP PROCEDURE IF EXISTS sp_auth_guest_create//
CREATE PROCEDURE sp_auth_guest_create(IN p_user_id CHAR(36),IN p_email VARCHAR(254),IN p_display_name VARCHAR(100),IN p_password_hash VARCHAR(512),IN p_username VARCHAR(32))
BEGIN
 DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
 START TRANSACTION;
 INSERT INTO Users(UserId,Email,DisplayName,PasswordHash,IsActive,IsGuest,UserRole,CreatedUtc,Username)
 VALUES(p_user_id,p_email,p_display_name,p_password_hash,1,1,1,UTC_TIMESTAMP(),p_username);
 INSERT INTO CaseOpeningUnlockedCases(UserId,CaseKey,UnlockedUtc) VALUES(p_user_id,'kilowatt',UTC_TIMESTAMP());
 INSERT INTO CaseOpeningOwnedCases(UserId,CaseKey,Quantity,UpdatedUtc) VALUES(p_user_id,'kilowatt',25,UTC_TIMESTAMP(6));
 INSERT INTO CaseOpeningInventoryCapacity(UserId,BaseCapacity,UpdatedUtc) VALUES(p_user_id,1000,UTC_TIMESTAMP(6));
 INSERT INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc) VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());
 COMMIT;
END//
DELIMITER ;

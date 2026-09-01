-- Case Tycoon social profiles. AccountId is the friendly public identifier; UserId remains the internal identity.
ALTER TABLE Users ADD COLUMN IF NOT EXISTS AccountId BIGINT UNSIGNED NULL;
ALTER TABLE Users ADD COLUMN IF NOT EXISTS Username VARCHAR(32) NULL COLLATE utf8mb4_unicode_ci;

SET @next_account_id := 10000000;
UPDATE Users SET AccountId = (@next_account_id := @next_account_id + 1) WHERE AccountId IS NULL ORDER BY CreatedUtc, UserId;
UPDATE Users
SET Username = CONCAT('player', AccountId)
WHERE Username IS NULL OR TRIM(Username) = '';

CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_AccountId ON Users(AccountId);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Username ON Users(Username);
ALTER TABLE Users MODIFY AccountId BIGINT UNSIGNED NOT NULL AUTO_INCREMENT;

CREATE TABLE IF NOT EXISTS UserFriends (
    UserId CHAR(36) NOT NULL,
    FriendUserId CHAR(36) NOT NULL,
    CreatedUtc DATETIME(6) NOT NULL DEFAULT UTC_TIMESTAMP(6),
    PRIMARY KEY(UserId, FriendUserId),
    CONSTRAINT FK_UserFriends_User FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserFriends_Friend FOREIGN KEY(FriendUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT CK_UserFriends_NotSelf CHECK(BINARY UserId <> BINARY FriendUserId),
    KEY IX_UserFriends_Friend(FriendUserId)
) ENGINE=InnoDB COLLATE=utf8mb4_unicode_ci;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_social_profile_get//
CREATE PROCEDURE sp_social_profile_get(IN p_viewer_user_id CHAR(36), IN p_target_user_id CHAR(36))
BEGIN
    SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
           COALESCE(presence.LastSeenUtc >= UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,
           EXISTS(SELECT 1 FROM UserFriends f WHERE BINARY f.UserId=BINARY p_viewer_user_id AND BINARY f.FriendUserId=BINARY u.UserId) IsFriend
    FROM Users u
    LEFT JOIN AppSettings setting ON setting.UserId=u.UserId AND setting.SettingKey='CaseProfileEmoji'
    LEFT JOIN UserLivePresence presence ON presence.UserId=u.UserId
    WHERE BINARY u.UserId=BINARY p_target_user_id AND u.IsActive=1;
END//
DROP PROCEDURE IF EXISTS sp_social_friends_get//
CREATE PROCEDURE sp_social_friends_get(IN p_user_id CHAR(36))
BEGIN
    SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
           COALESCE(presence.LastSeenUtc >= UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,1 IsFriend
    FROM UserFriends f JOIN Users u ON u.UserId=f.FriendUserId
    LEFT JOIN AppSettings setting ON setting.UserId=u.UserId AND setting.SettingKey='CaseProfileEmoji'
    LEFT JOIN UserLivePresence presence ON presence.UserId=u.UserId
    WHERE BINARY f.UserId=BINARY p_user_id AND u.IsActive=1
    ORDER BY IsOnline DESC,u.DisplayName,u.AccountId LIMIT 200;
END//
DROP PROCEDURE IF EXISTS sp_social_users_search//
CREATE PROCEDURE sp_social_users_search(IN p_user_id CHAR(36),IN p_query VARCHAR(100),IN p_limit INT)
BEGIN
    SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
           COALESCE(presence.LastSeenUtc >= UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,
           EXISTS(SELECT 1 FROM UserFriends f WHERE BINARY f.UserId=BINARY p_user_id AND BINARY f.FriendUserId=BINARY u.UserId) IsFriend
    FROM Users u
    LEFT JOIN AppSettings setting ON setting.UserId=u.UserId AND setting.SettingKey='CaseProfileEmoji'
    LEFT JOIN UserLivePresence presence ON presence.UserId=u.UserId
    WHERE u.IsActive=1 AND BINARY u.UserId<>BINARY p_user_id
      AND (COALESCE(u.Username,CONCAT('player',u.AccountId)) LIKE CONCAT('%',p_query,'%') OR u.DisplayName LIKE CONCAT('%',p_query,'%') OR CAST(u.AccountId AS CHAR)=TRIM(LEADING '#' FROM p_query))
    ORDER BY (COALESCE(u.Username,CONCAT('player',u.AccountId))=p_query) DESC,(CAST(u.AccountId AS CHAR)=TRIM(LEADING '#' FROM p_query)) DESC,IsFriend DESC,IsOnline DESC,u.DisplayName
    LIMIT p_limit;
END//
DROP PROCEDURE IF EXISTS sp_social_friend_add//
CREATE PROCEDURE sp_social_friend_add(IN p_user_id CHAR(36),IN p_friend_user_id CHAR(36))
BEGIN
    IF NOT EXISTS(SELECT 1 FROM Users WHERE BINARY UserId=BINARY p_friend_user_id AND IsActive=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That player is unavailable.'; END IF;
    INSERT IGNORE INTO UserFriends(UserId,FriendUserId) VALUES(p_user_id,p_friend_user_id),(p_friend_user_id,p_user_id);
END//
DROP PROCEDURE IF EXISTS sp_social_friend_remove//
CREATE PROCEDURE sp_social_friend_remove(IN p_user_id CHAR(36),IN p_friend_user_id CHAR(36))
BEGIN
    DELETE FROM UserFriends WHERE (BINARY UserId=BINARY p_user_id AND BINARY FriendUserId=BINARY p_friend_user_id) OR (BINARY UserId=BINARY p_friend_user_id AND BINARY FriendUserId=BINARY p_user_id);
END//
DROP PROCEDURE IF EXISTS sp_social_online_counts_get//
CREATE PROCEDURE sp_social_online_counts_get(IN p_user_id CHAR(36))
BEGIN
    SELECT
      (SELECT COUNT(*) FROM UserLivePresence p JOIN Users u ON u.UserId=p.UserId WHERE u.IsActive=1 AND p.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE) GlobalCount,
      (SELECT COUNT(*) FROM UserFriends f JOIN UserLivePresence p ON p.UserId=f.FriendUserId JOIN Users u ON u.UserId=f.FriendUserId WHERE BINARY f.UserId=BINARY p_user_id AND u.IsActive=1 AND p.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE) FriendsCount;
END//
DELIMITER ;

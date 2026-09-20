-- Self-contained repair for Case Tycoon social profiles and friend requests.
-- Safe to rerun: it preserves users, confirmed friends and pending requests.
USE PersonalTools;
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;

ALTER TABLE Users ADD COLUMN IF NOT EXISTS AccountId BIGINT UNSIGNED NULL;
ALTER TABLE Users ADD COLUMN IF NOT EXISTS Username VARCHAR(32) COLLATE utf8mb4_unicode_ci NULL;

-- Recover cleanly from a partially applied earlier script before adding unique indexes.
UPDATE Users duplicateUser
JOIN Users keeper ON duplicateUser.AccountId=keeper.AccountId AND BINARY duplicateUser.UserId>BINARY keeper.UserId
SET duplicateUser.AccountId=NULL
WHERE duplicateUser.AccountId IS NOT NULL;
SELECT GREATEST(10000000,COALESCE(MAX(AccountId),10000000)) INTO @social_next_account_id FROM Users;
UPDATE Users
SET AccountId=(@social_next_account_id:=@social_next_account_id+1)
WHERE AccountId IS NULL
ORDER BY CreatedUtc,UserId;
UPDATE Users SET Username=CONCAT('player',AccountId) WHERE Username IS NULL OR TRIM(Username)='';
UPDATE Users duplicateUser
JOIN Users keeper ON duplicateUser.Username=keeper.Username AND BINARY duplicateUser.UserId>BINARY keeper.UserId
SET duplicateUser.Username=CONCAT('player',duplicateUser.AccountId);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_AccountId ON Users(AccountId);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Username ON Users(Username);
ALTER TABLE Users MODIFY COLUMN AccountId BIGINT UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE Users MODIFY COLUMN Username VARCHAR(32) COLLATE utf8mb4_unicode_ci NULL;

CREATE TABLE IF NOT EXISTS UserLivePresence(
    UserId CHAR(36) NOT NULL,
    LastSeenUtc DATETIME(6) NOT NULL,
    PRIMARY KEY(UserId),
    KEY IX_UserLivePresence_LastSeen(LastSeenUtc),
    CONSTRAINT FK_UserLivePresence_User FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) ENGINE=InnoDB COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS UserFriends(
    UserId CHAR(36) NOT NULL,
    FriendUserId CHAR(36) NOT NULL,
    CreatedUtc DATETIME(6) NOT NULL DEFAULT UTC_TIMESTAMP(6),
    PRIMARY KEY(UserId,FriendUserId),
    KEY IX_UserFriends_Friend(FriendUserId),
    CONSTRAINT FK_UserFriends_User FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserFriends_Friend FOREIGN KEY(FriendUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT CK_UserFriends_NotSelf CHECK(BINARY UserId<>BINARY FriendUserId)
) ENGINE=InnoDB COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS UserFriendRequests(
    RequesterUserId CHAR(36) NOT NULL,
    RecipientUserId CHAR(36) NOT NULL,
    CreatedUtc DATETIME(6) NOT NULL DEFAULT UTC_TIMESTAMP(6),
    PRIMARY KEY(RequesterUserId,RecipientUserId),
    KEY IX_UserFriendRequests_Recipient(RecipientUserId,CreatedUtc),
    CONSTRAINT FK_UserFriendRequests_Requester FOREIGN KEY(RequesterUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UserFriendRequests_Recipient FOREIGN KEY(RecipientUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT CK_UserFriendRequests_NotSelf CHECK(BINARY RequesterUserId<>BINARY RecipientUserId)
) ENGINE=InnoDB COLLATE=utf8mb4_unicode_ci;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_live_winners_presence_touch//
CREATE PROCEDURE sp_live_winners_presence_touch(IN p_user_id CHAR(36))
BEGIN
 INSERT INTO UserLivePresence(UserId,LastSeenUtc) VALUES(p_user_id,UTC_TIMESTAMP(6))
 ON DUPLICATE KEY UPDATE LastSeenUtc=VALUES(LastSeenUtc);
END//

DROP PROCEDURE IF EXISTS sp_social_profile_get//
CREATE PROCEDURE sp_social_profile_get(IN p_viewer_user_id CHAR(36),IN p_target_user_id CHAR(36))
BEGIN
 SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
  COALESCE(presence.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,
  EXISTS(SELECT 1 FROM UserFriends f WHERE BINARY f.UserId=BINARY p_viewer_user_id AND BINARY f.FriendUserId=BINARY u.UserId) IsFriend,
  EXISTS(SELECT 1 FROM UserFriendRequests r WHERE BINARY r.RequesterUserId=BINARY p_viewer_user_id AND BINARY r.RecipientUserId=BINARY u.UserId) HasOutgoingFriendRequest,
  EXISTS(SELECT 1 FROM UserFriendRequests r WHERE BINARY r.RequesterUserId=BINARY u.UserId AND BINARY r.RecipientUserId=BINARY p_viewer_user_id) HasIncomingFriendRequest
 FROM Users u
 LEFT JOIN AppSettings setting ON BINARY setting.UserId=BINARY u.UserId AND setting.SettingKey='CaseProfileEmoji'
 LEFT JOIN UserLivePresence presence ON BINARY presence.UserId=BINARY u.UserId
 WHERE BINARY u.UserId=BINARY p_target_user_id AND u.IsActive=1;
END//

DROP PROCEDURE IF EXISTS sp_social_friends_get//
CREATE PROCEDURE sp_social_friends_get(IN p_user_id CHAR(36))
BEGIN
 SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
  COALESCE(presence.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,1 IsFriend,0 HasOutgoingFriendRequest,0 HasIncomingFriendRequest
 FROM UserFriends f JOIN Users u ON BINARY u.UserId=BINARY f.FriendUserId
 LEFT JOIN AppSettings setting ON BINARY setting.UserId=BINARY u.UserId AND setting.SettingKey='CaseProfileEmoji'
 LEFT JOIN UserLivePresence presence ON BINARY presence.UserId=BINARY u.UserId
 WHERE BINARY f.UserId=BINARY p_user_id AND u.IsActive=1
 ORDER BY IsOnline DESC,u.DisplayName,u.AccountId LIMIT 200;
END//

DROP PROCEDURE IF EXISTS sp_social_friend_requests_get//
CREATE PROCEDURE sp_social_friend_requests_get(IN p_user_id CHAR(36))
BEGIN
 SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
  COALESCE(presence.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,0 IsFriend,0 HasOutgoingFriendRequest,1 HasIncomingFriendRequest
 FROM UserFriendRequests request JOIN Users u ON BINARY u.UserId=BINARY request.RequesterUserId
 LEFT JOIN AppSettings setting ON BINARY setting.UserId=BINARY u.UserId AND setting.SettingKey='CaseProfileEmoji'
 LEFT JOIN UserLivePresence presence ON BINARY presence.UserId=BINARY u.UserId
 WHERE BINARY request.RecipientUserId=BINARY p_user_id AND u.IsActive=1
 ORDER BY request.CreatedUtc DESC LIMIT 200;
END//

DROP PROCEDURE IF EXISTS sp_social_users_search//
CREATE PROCEDURE sp_social_users_search(IN p_user_id CHAR(36),IN p_query VARCHAR(100),IN p_limit INT)
BEGIN
 SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
  COALESCE(presence.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,
  EXISTS(SELECT 1 FROM UserFriends f WHERE BINARY f.UserId=BINARY p_user_id AND BINARY f.FriendUserId=BINARY u.UserId) IsFriend,
  EXISTS(SELECT 1 FROM UserFriendRequests r WHERE BINARY r.RequesterUserId=BINARY p_user_id AND BINARY r.RecipientUserId=BINARY u.UserId) HasOutgoingFriendRequest,
  EXISTS(SELECT 1 FROM UserFriendRequests r WHERE BINARY r.RequesterUserId=BINARY u.UserId AND BINARY r.RecipientUserId=BINARY p_user_id) HasIncomingFriendRequest
 FROM Users u
 LEFT JOIN AppSettings setting ON BINARY setting.UserId=BINARY u.UserId AND setting.SettingKey='CaseProfileEmoji'
 LEFT JOIN UserLivePresence presence ON BINARY presence.UserId=BINARY u.UserId
 WHERE u.IsActive=1 AND BINARY u.UserId<>BINARY p_user_id
  AND (COALESCE(u.Username,CONCAT('player',u.AccountId)) COLLATE utf8mb4_unicode_ci LIKE CONCAT('%',p_query,'%') COLLATE utf8mb4_unicode_ci
   OR u.DisplayName COLLATE utf8mb4_unicode_ci LIKE CONCAT('%',p_query,'%') COLLATE utf8mb4_unicode_ci
   OR BINARY CAST(u.AccountId AS CHAR)=BINARY TRIM(LEADING '#' FROM p_query))
 ORDER BY IsFriend DESC,HasIncomingFriendRequest DESC,HasOutgoingFriendRequest DESC,IsOnline DESC,u.DisplayName
 LIMIT p_limit;
END//

DROP PROCEDURE IF EXISTS sp_social_friend_add//
CREATE PROCEDURE sp_social_friend_add(IN p_user_id CHAR(36),IN p_friend_user_id CHAR(36))
BEGIN
 IF BINARY p_user_id=BINARY p_friend_user_id THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Choose another player.'; END IF;
 IF NOT EXISTS(SELECT 1 FROM Users WHERE BINARY UserId=BINARY p_friend_user_id AND IsActive=1) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That player is unavailable.'; END IF;
 IF EXISTS(SELECT 1 FROM UserFriends WHERE BINARY UserId=BINARY p_user_id AND BINARY FriendUserId=BINARY p_friend_user_id) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='You are already friends.'; END IF;
 IF EXISTS(SELECT 1 FROM UserFriendRequests WHERE BINARY RequesterUserId=BINARY p_friend_user_id AND BINARY RecipientUserId=BINARY p_user_id) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This player has already sent you a request. Accept it from your pending requests.'; END IF;
 INSERT IGNORE INTO UserFriendRequests(RequesterUserId,RecipientUserId,CreatedUtc) VALUES(p_user_id,p_friend_user_id,UTC_TIMESTAMP(6));
END//

DROP PROCEDURE IF EXISTS sp_social_friend_request_accept//
CREATE PROCEDURE sp_social_friend_request_accept(IN p_user_id CHAR(36),IN p_requester_user_id CHAR(36))
BEGIN
 DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
 START TRANSACTION;
 DELETE FROM UserFriendRequests WHERE BINARY RequesterUserId=BINARY p_requester_user_id AND BINARY RecipientUserId=BINARY p_user_id;
 IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That friend request is no longer pending.'; END IF;
 INSERT IGNORE INTO UserFriends(UserId,FriendUserId,CreatedUtc) VALUES(p_user_id,p_requester_user_id,UTC_TIMESTAMP(6)),(p_requester_user_id,p_user_id,UTC_TIMESTAMP(6));
 DELETE FROM UserFriendRequests WHERE BINARY RequesterUserId=BINARY p_user_id AND BINARY RecipientUserId=BINARY p_requester_user_id;
 COMMIT;
END//

DROP PROCEDURE IF EXISTS sp_social_friend_request_deny//
CREATE PROCEDURE sp_social_friend_request_deny(IN p_user_id CHAR(36),IN p_requester_user_id CHAR(36))
BEGIN
 DELETE FROM UserFriendRequests WHERE BINARY RequesterUserId=BINARY p_requester_user_id AND BINARY RecipientUserId=BINARY p_user_id;
 IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That friend request is no longer pending.'; END IF;
END//

DROP PROCEDURE IF EXISTS sp_social_friend_remove//
CREATE PROCEDURE sp_social_friend_remove(IN p_user_id CHAR(36),IN p_friend_user_id CHAR(36))
BEGIN
 DELETE FROM UserFriends WHERE (BINARY UserId=BINARY p_user_id AND BINARY FriendUserId=BINARY p_friend_user_id) OR (BINARY UserId=BINARY p_friend_user_id AND BINARY FriendUserId=BINARY p_user_id);
 DELETE FROM UserFriendRequests WHERE (BINARY RequesterUserId=BINARY p_user_id AND BINARY RecipientUserId=BINARY p_friend_user_id) OR (BINARY RequesterUserId=BINARY p_friend_user_id AND BINARY RecipientUserId=BINARY p_user_id);
END//

DROP PROCEDURE IF EXISTS sp_social_online_counts_get//
CREATE PROCEDURE sp_social_online_counts_get(IN p_user_id CHAR(36))
BEGIN
 SELECT
  (SELECT COUNT(*) FROM UserLivePresence p JOIN Users u ON BINARY u.UserId=BINARY p.UserId WHERE u.IsActive=1 AND p.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE) GlobalCount,
  (SELECT COUNT(*) FROM UserFriends f JOIN UserLivePresence p ON BINARY p.UserId=BINARY f.FriendUserId JOIN Users u ON BINARY u.UserId=BINARY f.FriendUserId WHERE BINARY f.UserId=BINARY p_user_id AND u.IsActive=1 AND p.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE) FriendsCount;
END//
DELIMITER ;

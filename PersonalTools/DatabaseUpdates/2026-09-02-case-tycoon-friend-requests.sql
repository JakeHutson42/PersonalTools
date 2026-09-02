-- Confirmed friendships remain in UserFriends. New relationships begin in UserFriendRequests.
USE PersonalTools;

CREATE TABLE IF NOT EXISTS UserFriendRequests (
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
DROP PROCEDURE IF EXISTS sp_social_profile_get//
CREATE PROCEDURE sp_social_profile_get(IN p_viewer_user_id CHAR(36),IN p_target_user_id CHAR(36))
BEGIN
 SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
  COALESCE(presence.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,
  EXISTS(SELECT 1 FROM UserFriends f WHERE BINARY f.UserId=BINARY p_viewer_user_id AND BINARY f.FriendUserId=BINARY u.UserId) IsFriend,
  EXISTS(SELECT 1 FROM UserFriendRequests r WHERE BINARY r.RequesterUserId=BINARY p_viewer_user_id AND BINARY r.RecipientUserId=BINARY u.UserId) HasOutgoingFriendRequest,
  EXISTS(SELECT 1 FROM UserFriendRequests r WHERE BINARY r.RequesterUserId=BINARY u.UserId AND BINARY r.RecipientUserId=BINARY p_viewer_user_id) HasIncomingFriendRequest
 FROM Users u LEFT JOIN AppSettings setting ON setting.UserId=u.UserId AND setting.SettingKey='CaseProfileEmoji'
 LEFT JOIN UserLivePresence presence ON presence.UserId=u.UserId
 WHERE BINARY u.UserId=BINARY p_target_user_id AND u.IsActive=1;
END//

DROP PROCEDURE IF EXISTS sp_social_friends_get//
CREATE PROCEDURE sp_social_friends_get(IN p_user_id CHAR(36))
BEGIN
 SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
  COALESCE(presence.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,1 IsFriend,0 HasOutgoingFriendRequest,0 HasIncomingFriendRequest
 FROM UserFriends f JOIN Users u ON u.UserId=f.FriendUserId
 LEFT JOIN AppSettings setting ON setting.UserId=u.UserId AND setting.SettingKey='CaseProfileEmoji'
 LEFT JOIN UserLivePresence presence ON presence.UserId=u.UserId
 WHERE BINARY f.UserId=BINARY p_user_id AND u.IsActive=1 ORDER BY IsOnline DESC,u.DisplayName,u.AccountId LIMIT 200;
END//

DROP PROCEDURE IF EXISTS sp_social_friend_requests_get//
CREATE PROCEDURE sp_social_friend_requests_get(IN p_user_id CHAR(36))
BEGIN
 SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
  COALESCE(presence.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,0 IsFriend,0 HasOutgoingFriendRequest,1 HasIncomingFriendRequest
 FROM UserFriendRequests request JOIN Users u ON u.UserId=request.RequesterUserId
 LEFT JOIN AppSettings setting ON setting.UserId=u.UserId AND setting.SettingKey='CaseProfileEmoji'
 LEFT JOIN UserLivePresence presence ON presence.UserId=u.UserId
 WHERE BINARY request.RecipientUserId=BINARY p_user_id AND u.IsActive=1 ORDER BY request.CreatedUtc DESC LIMIT 200;
END//

DROP PROCEDURE IF EXISTS sp_social_users_search//
CREATE PROCEDURE sp_social_users_search(IN p_user_id CHAR(36),IN p_query VARCHAR(100),IN p_limit INT)
BEGIN
 SELECT u.UserId,u.AccountId,COALESCE(u.Username,CONCAT('player',u.AccountId)) Username,u.DisplayName,COALESCE(setting.SettingValue,'😎') Avatar,
  COALESCE(presence.LastSeenUtc>=UTC_TIMESTAMP(6)-INTERVAL 2 MINUTE,0) IsOnline,presence.LastSeenUtc,
  EXISTS(SELECT 1 FROM UserFriends f WHERE BINARY f.UserId=BINARY p_user_id AND BINARY f.FriendUserId=BINARY u.UserId) IsFriend,
  EXISTS(SELECT 1 FROM UserFriendRequests r WHERE BINARY r.RequesterUserId=BINARY p_user_id AND BINARY r.RecipientUserId=BINARY u.UserId) HasOutgoingFriendRequest,
  EXISTS(SELECT 1 FROM UserFriendRequests r WHERE BINARY r.RequesterUserId=BINARY u.UserId AND BINARY r.RecipientUserId=BINARY p_user_id) HasIncomingFriendRequest
 FROM Users u LEFT JOIN AppSettings setting ON setting.UserId=u.UserId AND setting.SettingKey='CaseProfileEmoji'
 LEFT JOIN UserLivePresence presence ON presence.UserId=u.UserId
 WHERE u.IsActive=1 AND BINARY u.UserId<>BINARY p_user_id
  AND (COALESCE(u.Username,CONCAT('player',u.AccountId)) LIKE CONCAT('%',p_query,'%') OR u.DisplayName LIKE CONCAT('%',p_query,'%') OR CAST(u.AccountId AS CHAR)=TRIM(LEADING '#' FROM p_query))
 ORDER BY IsFriend DESC,HasIncomingFriendRequest DESC,HasOutgoingFriendRequest DESC,IsOnline DESC,u.DisplayName LIMIT p_limit;
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
DELIMITER ;

USE PersonalTools;

-- Existing installations can contain user/profile tables created under different
-- utf8mb4 collations. Keep social search independent of those legacy defaults.
DELIMITER //
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
DELIMITER ;

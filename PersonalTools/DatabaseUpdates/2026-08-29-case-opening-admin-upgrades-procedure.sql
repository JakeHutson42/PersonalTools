USE PersonalTools;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_opening_upgrades_dev_set//
CREATE PROCEDURE sp_case_opening_upgrades_dev_set
(
    IN p_user_id CHAR(36),
    IN p_skip_animation_unlocked TINYINT(1),
    IN p_multi_open_level TINYINT UNSIGNED,
    IN p_open_speed_level TINYINT UNSIGNED
)
BEGIN
    INSERT IGNORE INTO CaseOpeningProgress(UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel,UpdatedUtc)
    VALUES(p_user_id,0,0,0,0,0,0,UTC_TIMESTAMP());

    UPDATE CaseOpeningProgress
    SET SkipAnimationUnlocked=p_skip_animation_unlocked,
        MultiOpenLevel=p_multi_open_level,
        OpenSpeedLevel=p_open_speed_level,
        UpdatedUtc=UTC_TIMESTAMP()
    WHERE UserId=p_user_id;

    SELECT UserId,Stars,GbpPence,Xp,SkipAnimationUnlocked,MultiOpenLevel,OpenSpeedLevel
    FROM CaseOpeningProgress
    WHERE UserId=p_user_id;
END//
DELIMITER ;

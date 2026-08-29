-- The dual-economy migration added CaseOpeningProgress.GbpPence. Recreate this
-- legacy writer so its result matches CaseOpeningData.ReadProgress.
DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_opening_xp_add//
CREATE PROCEDURE sp_case_opening_xp_add
(
    IN p_user_id CHAR(36),
    IN p_xp_delta INT
)
BEGIN
    INSERT IGNORE INTO CaseOpeningProgress
    (
        UserId,
        Stars,
        GbpPence,
        Xp,
        SkipAnimationUnlocked,
        MultiOpenLevel,
        OpenSpeedLevel,
        UpdatedUtc
    )
    VALUES
    (
        p_user_id,
        0,
        0,
        0,
        0,
        0,
        0,
        UTC_TIMESTAMP()
    );

    UPDATE CaseOpeningProgress
    SET Xp = Xp + p_xp_delta,
        UpdatedUtc = UTC_TIMESTAMP()
    WHERE UserId = p_user_id;

    SELECT UserId,
           Stars,
           GbpPence,
           Xp,
           SkipAnimationUnlocked,
           MultiOpenLevel,
           OpenSpeedLevel
    FROM CaseOpeningProgress
    WHERE UserId = p_user_id;
END//

DELIMITER ;

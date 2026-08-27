USE PersonalTools;

DELIMITER //

DROP PROCEDURE IF EXISTS sp_case_opening_collections_get//
CREATE PROCEDURE sp_case_opening_collections_get(IN p_user_id CHAR(36))
BEGIN
    SELECT
        CollectionId,
        UserId,
        CaseKey,
        SourceItemId,
        FirstObtainedUtc
    FROM CaseOpeningCollection
    WHERE BINARY UserId = BINARY p_user_id
    ORDER BY FirstObtainedUtc, CollectionId;
END//

DELIMITER ;

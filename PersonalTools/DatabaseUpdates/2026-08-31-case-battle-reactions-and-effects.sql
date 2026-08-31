CREATE TABLE IF NOT EXISTS CaseBattleReactionUnlocks (
    UserId CHAR(36) NOT NULL,
    ReactionKey VARCHAR(50) NOT NULL,
    UnlockedUtc DATETIME(6) NOT NULL DEFAULT UTC_TIMESTAMP(6),
    PRIMARY KEY (UserId, ReactionKey),
    CONSTRAINT FK_CaseBattleReactionUnlocks_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) ENGINE=InnoDB COLLATE=utf8mb4_unicode_ci;

DELIMITER //
DROP PROCEDURE IF EXISTS sp_case_battle_reaction_unlocks_get//
CREATE PROCEDURE sp_case_battle_reaction_unlocks_get(IN p_user_id CHAR(36))
BEGIN
    SELECT ReactionKey FROM CaseBattleReactionUnlocks WHERE UserId=p_user_id ORDER BY UnlockedUtc;
END//

DROP PROCEDURE IF EXISTS sp_case_battle_reaction_purchase//
CREATE PROCEDURE sp_case_battle_reaction_purchase(IN p_user_id CHAR(36), IN p_reaction_key VARCHAR(50), IN p_cost_stars INT, IN p_cost_gbp_pence BIGINT)
BEGIN
    DECLARE v_mode VARCHAR(20) DEFAULT 'stars';
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    IF EXISTS(SELECT 1 FROM CaseBattleReactionUnlocks WHERE UserId=p_user_id AND ReactionKey=p_reaction_key) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='That battle reaction is already unlocked.';
    END IF;
    SELECT EconomyMode INTO v_mode FROM CaseOpeningGameSettings WHERE Id=1;
    IF LOWER(v_mode)='gbp' THEN
        UPDATE CaseOpeningProgress SET GbpPence=GbpPence-p_cost_gbp_pence,UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND GbpPence>=p_cost_gbp_pence;
    ELSE
        UPDATE CaseOpeningProgress SET Stars=Stars-p_cost_stars,UpdatedUtc=UTC_TIMESTAMP() WHERE UserId=p_user_id AND Stars>=p_cost_stars;
    END IF;
    IF ROW_COUNT()=0 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='There is not enough currency to unlock this battle reaction.'; END IF;
    INSERT INTO CaseBattleReactionUnlocks(UserId,ReactionKey,UnlockedUtc) VALUES(p_user_id,p_reaction_key,UTC_TIMESTAMP(6));
    COMMIT;
END//
DELIMITER ;

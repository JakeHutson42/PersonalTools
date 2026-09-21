-- PersonalTools SQL bundle for 2026-09-20.
-- This is the only SQL migration created today. Select the PersonalTools database,
-- open a NEW HeidiSQL query tab, load this entire file, and execute it with F9.
-- Do not paste it into HeidiSQL's procedure editor or run only a selection.
-- It requires Users(UserId). The !! delimiter keeps procedure bodies intact.
-- Safe to rerun: tables use IF NOT EXISTS and procedures are replaced.
-- Chess is independent of Case Tycoon rewards; no existing data is deleted.
CREATE TABLE IF NOT EXISTS ChessGames (
    GameId CHAR(36) NOT NULL PRIMARY KEY,
    WhiteUserId CHAR(36) NOT NULL,
    BlackUserId CHAR(36) NULL,
    Mode VARCHAR(8) NOT NULL,
    Status VARCHAR(12) NOT NULL,
    Fen VARCHAR(120) NOT NULL,
    Pgn TEXT NOT NULL,
    Version INT UNSIGNED NOT NULL DEFAULT 0,
    Revision INT UNSIGNED NOT NULL DEFAULT 0,
    Result VARCHAR(16) NULL,
    Difficulty SMALLINT UNSIGNED NOT NULL DEFAULT 500,
    CreatedUtc DATETIME(6) NOT NULL,
    UpdatedUtc DATETIME(6) NOT NULL,
    CONSTRAINT FK_ChessGames_White FOREIGN KEY (WhiteUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_ChessGames_Black FOREIGN KEY (BlackUserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT CK_ChessGames_Mode CHECK (Mode IN ('ai','pvp')),
    CONSTRAINT CK_ChessGames_Status CHECK (Status IN ('waiting','active','finished')),
    KEY IX_ChessGames_WhiteUpdated (WhiteUserId, UpdatedUtc),
    KEY IX_ChessGames_BlackUpdated (BlackUserId, UpdatedUtc)
) ENGINE=InnoDB;

-- Existing chess installations receive the optimistic-concurrency token too.
ALTER TABLE ChessGames ADD COLUMN IF NOT EXISTS Revision INT UNSIGNED NOT NULL DEFAULT 0 AFTER Version;
-- Existing four-level games keep their stored values; the app maps them to legacy levels.
ALTER TABLE ChessGames MODIFY COLUMN Difficulty SMALLINT UNSIGNED NOT NULL DEFAULT 500;

CREATE TABLE IF NOT EXISTS ChessMoves (
    GameId CHAR(36) NOT NULL,
    Ply INT UNSIGNED NOT NULL,
    FromSquare CHAR(2) NOT NULL,
    ToSquare CHAR(2) NOT NULL,
    Promotion CHAR(1) NULL,
    CapturedPiece VARCHAR(8) NULL,
    San VARCHAR(24) NOT NULL,
    PlayedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY (GameId, Ply),
    CONSTRAINT FK_ChessMoves_Game FOREIGN KEY (GameId) REFERENCES ChessGames(GameId) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS ChessLessonProgress (
    UserId CHAR(36) NOT NULL,
    LessonKey VARCHAR(80) NOT NULL,
    CompletedUtc DATETIME(6) NOT NULL,
    PRIMARY KEY (UserId, LessonKey),
    CONSTRAINT FK_ChessLessonProgress_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) ENGINE=InnoDB;

DELIMITER !!

DROP PROCEDURE IF EXISTS sp_chess_games_list!!
CREATE PROCEDURE sp_chess_games_list(IN p_user_id CHAR(36))
BEGIN
    SELECT * FROM ChessGames WHERE WhiteUserId=p_user_id OR BlackUserId=p_user_id ORDER BY UpdatedUtc DESC LIMIT 30;
END!!

DROP PROCEDURE IF EXISTS sp_chess_game_get!!
CREATE PROCEDURE sp_chess_game_get(IN p_game_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT * FROM ChessGames WHERE GameId=p_game_id AND (WhiteUserId=p_user_id OR BlackUserId=p_user_id);
END!!

DROP PROCEDURE IF EXISTS sp_chess_moves_list!!
CREATE PROCEDURE sp_chess_moves_list(IN p_game_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    SELECT m.* FROM ChessMoves m INNER JOIN ChessGames g ON g.GameId=m.GameId
    WHERE m.GameId=p_game_id AND (g.WhiteUserId=p_user_id OR g.BlackUserId=p_user_id) ORDER BY m.Ply;
END!!

DROP PROCEDURE IF EXISTS sp_chess_game_create!!
CREATE PROCEDURE sp_chess_game_create(IN p_game_id CHAR(36), IN p_user_id CHAR(36), IN p_mode VARCHAR(8), IN p_difficulty INT, IN p_fen VARCHAR(120))
BEGIN
    IF p_mode NOT IN ('ai','pvp') OR NOT (
        (p_difficulty BETWEEN 100 AND 2400 AND MOD(p_difficulty,100)=0)
        OR p_difficulty BETWEEN 1 AND 4) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Invalid game options.';
    END IF;
    INSERT INTO ChessGames(GameId,WhiteUserId,Mode,Status,Fen,Pgn,Version,Difficulty,CreatedUtc,UpdatedUtc)
    VALUES(p_game_id,p_user_id,p_mode,IF(p_mode='ai','active','waiting'),p_fen,'',0,p_difficulty,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
END!!

DROP PROCEDURE IF EXISTS sp_chess_game_join!!
CREATE PROCEDURE sp_chess_game_join(IN p_game_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    UPDATE ChessGames SET BlackUserId=p_user_id,Status='active',UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE GameId=p_game_id AND Mode='pvp' AND Status='waiting' AND WhiteUserId<>p_user_id AND BlackUserId IS NULL;
    IF ROW_COUNT()<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This invitation is no longer available.'; END IF;
END!!

DROP PROCEDURE IF EXISTS sp_chess_move_apply!!
CREATE PROCEDURE sp_chess_move_apply(
    IN p_game_id CHAR(36), IN p_user_id CHAR(36), IN p_version INT, IN p_revision INT,
    IN p_old_fen VARCHAR(120), IN p_turn CHAR(1), IN p_from CHAR(2), IN p_to CHAR(2),
    IN p_promotion CHAR(1), IN p_captured VARCHAR(8), IN p_san VARCHAR(24),
    IN p_fen VARCHAR(120), IN p_pgn TEXT, IN p_result VARCHAR(16))
BEGIN
    DECLARE v_white CHAR(36);
    DECLARE v_black CHAR(36);
    DECLARE v_mode VARCHAR(8);
    DECLARE v_status VARCHAR(12);
    DECLARE v_fen VARCHAR(120);
    DECLARE v_version INT;
    DECLARE v_revision INT;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    -- Lock the game and verify the server-derived position before writing the next ply.
    SELECT WhiteUserId,BlackUserId,Mode,Status,Fen,Version,Revision INTO v_white,v_black,v_mode,v_status,v_fen,v_version,v_revision
    FROM ChessGames WHERE GameId=p_game_id FOR UPDATE;
    IF v_status<>'active' OR v_version<>p_version OR v_revision<>p_revision OR v_fen<>p_old_fen OR
       SUBSTRING_INDEX(SUBSTRING_INDEX(v_fen,' ',2),' ',-1)<>p_turn THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Game changed. Refresh the board.';
    END IF;
    IF (p_turn='w' AND v_white<>p_user_id) OR
       (p_turn='b' AND NOT ((v_mode='ai' AND v_white=p_user_id) OR (v_mode='pvp' AND v_black=p_user_id))) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='It is not your turn.';
    END IF;
    UPDATE ChessGames SET Fen=p_fen,Pgn=p_pgn,Version=Version+1,Revision=Revision+1,
        Status=IF(p_result IS NULL,'active','finished'),Result=p_result,UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE GameId=p_game_id;
    INSERT INTO ChessMoves(GameId,Ply,FromSquare,ToSquare,Promotion,CapturedPiece,San,PlayedUtc)
    VALUES(p_game_id,p_version+1,p_from,p_to,p_promotion,p_captured,p_san,UTC_TIMESTAMP(6));
    COMMIT;
END!!

DROP PROCEDURE IF EXISTS sp_chess_game_undo!!
CREATE PROCEDURE sp_chess_game_undo(
    IN p_game_id CHAR(36), IN p_user_id CHAR(36), IN p_version INT, IN p_revision INT,
    IN p_new_version INT, IN p_fen VARCHAR(120), IN p_pgn TEXT)
BEGIN
    DECLARE v_version INT;
    DECLARE v_revision INT;
    DECLARE v_mode VARCHAR(8);
    DECLARE v_owner CHAR(36);
    DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
    START TRANSACTION;
    SELECT Version,Revision,Mode,WhiteUserId INTO v_version,v_revision,v_mode,v_owner
    FROM ChessGames WHERE GameId=p_game_id FOR UPDATE;
    IF v_owner<>p_user_id OR v_mode<>'ai' OR v_version<>p_version OR v_revision<>p_revision OR
       p_new_version>=v_version OR p_new_version<0 OR v_version-p_new_version>2 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Game changed or undo is unavailable.';
    END IF;
    DELETE FROM ChessMoves WHERE GameId=p_game_id AND Ply>p_new_version;
    UPDATE ChessGames SET Fen=p_fen,Pgn=p_pgn,Version=p_new_version,Revision=Revision+1,
        Status='active',Result=NULL,UpdatedUtc=UTC_TIMESTAMP(6) WHERE GameId=p_game_id;
    COMMIT;
END!!

DROP PROCEDURE IF EXISTS sp_chess_lessons_completed!!
CREATE PROCEDURE sp_chess_lessons_completed(IN p_user_id CHAR(36))
BEGIN
    SELECT LessonKey,CompletedUtc FROM ChessLessonProgress WHERE UserId=p_user_id ORDER BY CompletedUtc;
END!!

DROP PROCEDURE IF EXISTS sp_chess_lesson_complete!!
CREATE PROCEDURE sp_chess_lesson_complete(IN p_user_id CHAR(36), IN p_lesson_key VARCHAR(80))
BEGIN
    INSERT INTO ChessLessonProgress(UserId,LessonKey,CompletedUtc)
    VALUES(p_user_id,p_lesson_key,UTC_TIMESTAMP(6))
    ON DUPLICATE KEY UPDATE LessonKey=VALUES(LessonKey);
END!!

DROP PROCEDURE IF EXISTS sp_chess_game_resign!!
CREATE PROCEDURE sp_chess_game_resign(IN p_game_id CHAR(36), IN p_user_id CHAR(36))
BEGIN
    UPDATE ChessGames SET Status='finished',Result=IF(WhiteUserId=p_user_id,'black','white'),UpdatedUtc=UTC_TIMESTAMP(6)
    WHERE GameId=p_game_id AND Status='active' AND (WhiteUserId=p_user_id OR BlackUserId=p_user_id);
    IF ROW_COUNT()<>1 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='This game cannot be resigned.'; END IF;
END!!

DELIMITER ;
